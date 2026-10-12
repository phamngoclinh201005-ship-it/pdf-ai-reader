
using System.Threading.RateLimiting;
using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

using PaperFlow.Api.Services;
using PaperFlow.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Railway cấp PORT động cho ứng dụng
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Controllers
builder.Services.AddControllers();

// Authentication
builder.Services.AddSingleton<AuthStore>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<PasswordResetService>();
builder.Services.AddSingleton<LibraryStore>();

// HTTPS delivery works on Render Free; SMTP remains optional for local use.
if (string.Equals(builder.Configuration["Email:Provider"], "Gmail", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddScoped<IAuthEmailService, GmailEmailService>();
else
    builder.Services.AddHttpClient<IAuthEmailService, BrevoEmailService>(client => client.Timeout = TimeSpan.FromSeconds(20));

// Frontend URL
var frontendOrigin =
    builder.Configuration["Frontend:Origin"]
    ?? "http://localhost:3000";

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 15,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("ai", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

// Gemini AI
builder.Services.AddHttpClient<GeminiService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(90);
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("NuxtFrontend", policy =>
    {
        policy.WithOrigins(frontendOrigin)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// MongoDB History
builder.Services.AddSingleton<MongoHistoryService>();

// Forwarded Headers (Railway Reverse Proxy)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;

    // Chỉ tin forwarded headers từ proxy đã xác thực.
    // Cần cấu hình KnownProxies/KnownNetworks
    // theo hạ tầng Railway trước khi sử dụng.
});

var app = builder.Build();

// Middleware
app.UseMiddleware<AiExceptionMiddleware>();

app.Logger.LogInformation("Initializing MongoDB authentication and library indexes before listening.");
using (var startupTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(45)))
{
    try
    {
        await app.Services.GetRequiredService<AuthStore>().EnsureIndexesAsync(startupTimeout.Token);
        await app.Services.GetRequiredService<LibraryStore>().EnsureIndexesAsync(startupTimeout.Token);
        app.Logger.LogInformation("MongoDB indexes initialized successfully.");
    }
    catch (Exception error)
    {
        app.Logger.LogCritical("Database startup failed ({ErrorType}). Check MongoDB credentials, Atlas network access and DNS. No connection string is logged.", error.GetType().FullName);
        throw new InvalidOperationException("Database initialization failed. See startup diagnostics above.");
    }
}
app.UseCors("NuxtFrontend");

// Origin protection
app.Use(async (context, next) =>
{
    if ((context.Request.Path.StartsWithSegments("/api/auth") || context.Request.Path.StartsWithSegments("/api/library")) &&
        !HttpMethods.IsGet(context.Request.Method) &&
        !HttpMethods.IsOptions(context.Request.Method))
    {
        var origin = context.Request.Headers.Origin.ToString();

        if ((!string.IsNullOrEmpty(origin) &&
             origin != frontendOrigin) ||
            (string.IsNullOrEmpty(origin) &&
             context.Request.Headers.ContainsKey("Cookie")))
        {
            context.Response.StatusCode = 403;

            await context.Response.WriteAsJsonAsync(new
            {
                message = "Origin not allowed"
            });

            return;
        }
    }

    await next();
});

app.UseRateLimiter();

app.MapControllers();

app.Run();
