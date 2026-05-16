namespace ZeroX2C.Blog.API.Modules.Users.Auth;

public interface IAuthenticationContextManager
{
    IAuthenticationContext Current { get; }

    IAuthenticationScope As(AuthenticationData authenticationData);
}
