
using DataAccess.Context;
using System.Security.Claims;

namespace ApiApp.Middlewares;

public class AuthenticationMiddleware : IMiddleware
{
    private readonly DataContext _dataContext;

    public AuthenticationMiddleware(DataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var identity = context.User.Identity as ClaimsIdentity;

        if (identity != null)
        {
            if (int.TryParse(identity.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
            {
                // Add the user ID to the HttpContext
                context.Items["UserId"] = userId;
                context.Items["User"] = _dataContext.Users.Find(userId);
            }
        }

        await next(context);
    }
}
