namespace ZeroX2C.Blog.API.Modules.Users.Auth;

public sealed class AuthenticationContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext httpContext,
        IAuthenticationContextManager authenticationContextManager
    )
    {
        var authenticationData = AuthenticationData.FromPrincipal(httpContext.User);
        using (authenticationContextManager.As(authenticationData))
        {
            await next(httpContext);
        }
    }
}
