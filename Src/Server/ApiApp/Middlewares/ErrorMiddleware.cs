
using ApiApp.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace ApiApp.Middlewares;

//public class ErrorMiddleware(ILogger<ErrorMiddleware> _logger) : IMiddleware
public class ErrorMiddleware() : IMiddleware
{
    //private readonly ILogger<ErrorMiddleware> _logger;

    //public ErrorMiddleware(
    //    ILogger<ErrorMiddleware> logger) =>
    //    _logger = logger;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {

            //_logger.LogError(ex, "Error processing the request {protocol} {method} {path}",
            //    context.Request.Protocol,
            //    context.Request.Method,
            //    context.Request.Path);

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            ProblemDetails problem = new()
            {
                Status = context.Response.StatusCode,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "Internal Server Error",
                Detail = $"Undefined Internal error. Logged at time: {DateTime.Now:yyyyMMdd HH:mm:ss}.",
            };


            if (ex is ClientResponseException)
            {
                int statusCode = (int)(ex as ClientResponseException)!.StatusCode;

                context.Response.StatusCode = statusCode;
                problem.Status = statusCode;
                problem.Detail += $" \r\n {ex.StackTrace}";
            }

            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
