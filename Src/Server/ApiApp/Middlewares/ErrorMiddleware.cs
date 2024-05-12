
using ApiApp.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace ApiApp.Middlewares;

//public class ErrorMiddleware(ILogger<ErrorMiddleware> _logger) : IMiddleware
public class ErrorMiddleware : IMiddleware
{
    private readonly ILogger<ErrorMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public ErrorMiddleware(ILogger<ErrorMiddleware> logger,
                           IWebHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {

            _logger.LogError(ex, "Error processing the request {protocol} {method} {path}",
                context.Request.Protocol,
                context.Request.Method,
                context.Request.Path);

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            ProblemDetails problem = new()
            {
                Status = context.Response.StatusCode,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "Undefined Internal error. Logged at time: {DateTime.Now:yyyyMMdd HH:mm:ss}.",
                Detail = ex.Message
            };


            if (ex is ClientResponseException)
            {
                int statusCode = (int)(ex as ClientResponseException)!.StatusCode;

                context.Response.StatusCode = statusCode;
                problem.Status = statusCode;
            }
            
            if (_env.IsDevelopment())
            {
                problem.Detail += $"   ----   \r\n {ex.StackTrace}";
            }

            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
