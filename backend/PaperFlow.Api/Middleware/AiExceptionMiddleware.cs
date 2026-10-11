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
            logger.LogError(exception, "AI request failed");
            if (context.Response.HasStarted) throw;
            context.Response.StatusCode = exception is HttpRequestException ? 502 : 500;
            await context.Response.WriteAsJsonAsync(new
            {
                message = "Không thể xử lý yêu cầu AI. Vui lòng thử lại."
            });
        }
    }
}
