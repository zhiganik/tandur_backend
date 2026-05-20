using Serilog.Context;
using System.Security.Claims;

namespace Api.Middleware;

public class LogEnrichmentMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        var role   = context.User.FindFirstValue(ClaimTypes.Role)           ?? "none";

        using (LogContext.PushProperty("UserId",    userId))
        using (LogContext.PushProperty("UserRole",  role))
        using (LogContext.PushProperty("RequestId", context.TraceIdentifier))
        {
            await next(context);
        }
    }
}
