namespace ZeroX2C.Blog.API.Modules.Users.Auth;

internal static class AuthenticationContextManagerExtensions
{
    public static IAuthenticationScope AsSystem(this IAuthenticationContextManager self)
    {
        return self.As(KnownUsers.System);
    }
}
