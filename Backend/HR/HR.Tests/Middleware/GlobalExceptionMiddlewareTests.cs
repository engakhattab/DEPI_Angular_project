using System.Text.Json;
using HR.API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace HR.Tests.Middleware;

public class GlobalExceptionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenUnexpectedExceptionOccurs_ReturnsGenericServerError()
    {
        const string sensitiveDetails = "sensitive database details";
        var middleware = new GlobalExceptionMiddleware(
            _ => throw new InvalidOperationException(sensitiveDetails),
            NullLogger<GlobalExceptionMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var payload = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("SERVER_ERROR", payload.RootElement.GetProperty("code").GetString());
        Assert.Equal("An unexpected error occurred.", payload.RootElement.GetProperty("message").GetString());
        Assert.DoesNotContain(sensitiveDetails, payload.RootElement.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
