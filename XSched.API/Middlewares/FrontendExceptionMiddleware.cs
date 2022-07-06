using Newtonsoft.Json;
using XSched.API.Models;

namespace XSched.API.Middlewares;

public class FrontendExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public FrontendExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next.Invoke(context);
        }
        catch (FrontendException ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, FrontendException exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";
        response.StatusCode = exception.StatusCode;
        await response.WriteAsync(JsonConvert.SerializeObject(new
        {
            errors = exception.Messages
        }));
    }
}

public static class ExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseFrontendExceptionMiddleware(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<FrontendExceptionMiddleware>();
    }
}