namespace PaperFlow.Api.Middleware;

public class AiExceptionMiddleware(RequestDelegate next, ILogger<AiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The caller disconnected; do not attempt to write a response.
        }
        catch (Exception exception)
        {
            logger.LogError("Request failed: {Path}; error type: {ErrorType}; trace ID: {TraceId}; stack: {StackTrace}",
                context.Request.Path, exception.GetType().FullName, context.TraceIdentifier, exception.StackTrace);
            if (context.Response.HasStarted) throw;
            context.Response.StatusCode = exception is MongoDB.Driver.MongoException or TimeoutException ? 503
                : exception is HttpRequestException ? 502 : 500;
            await context.Response.WriteAsJsonAsync(new
            {
                message = context.Response.StatusCode == 503
                    ? "Dịch vụ dữ liệu tạm thời không khả dụng. Vui lòng thử lại sau."
                    : "Không thể xử lý yêu cầu. Vui lòng thử lại.",
                traceId = context.TraceIdentifier
            });
        }
    }
}
