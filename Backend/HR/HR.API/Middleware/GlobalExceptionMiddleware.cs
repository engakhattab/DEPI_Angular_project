using HR.Domain.Exceptions;

namespace HR.API.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (NotFoundException ex)
        {
            await WriteError(context, 404, "NOT_FOUND", ex.Message);
        }
        catch (ConflictException ex)
        {
            await WriteError(context, 409, "CONFLICT", ex.Message);
        }
        catch (BusinessRuleException ex)
        {
            await WriteError(context, 422, "BUSINESS_RULE", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);
            await WriteError(context, 500, "SERVER_ERROR", "An unexpected error occurred.");
        }
    }

    private static async Task WriteError(
        HttpContext ctx, int status, string code, string message)
    {
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsJsonAsync(new { code, message });
    }
}
