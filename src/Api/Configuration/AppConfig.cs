using Api.Middleware;
using Microsoft.AspNetCore.Diagnostics;
using Serilog;
using System.Security.Claims;

namespace Api.Configuration;

public static class AppConfig
{
    public static IApplicationBuilder Configure(this WebApplication app)
    {
        app.UseSerilogRequestLogging(opts =>
        {
            opts.EnrichDiagnosticContext = (diagCtx, httpCtx) =>
            {
                var userId = httpCtx.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId is not null)
                    diagCtx.Set("UserId", userId);
            };
        });

        app.UseExceptionHandler(errApp => errApp.Run(async ctx =>
        {
            var ex     = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
            var logger = ctx.RequestServices.GetRequiredService<ILogger<WebApplication>>();
            logger.LogError(ex, "Unhandled exception on {Method} {Path}", ctx.Request.Method, ctx.Request.Path);

            ctx.Response.StatusCode  = 500;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(new { message = "An unexpected error occurred." });
        }));

        app.UseSwagger(c => c.RouteTemplate = "api/swagger/{documentName}/swagger.json");
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/api/swagger/v1/swagger.json", "v1");
            c.RoutePrefix = "api/swagger";
        });

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<LogEnrichmentMiddleware>();
        app.MapControllers();

        app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

        return app;
    }
}
