namespace ZeroX2C.Blog.API.Modules.Users.Auth;

public static class UserNormalization
{
    public static string NormalizeUsername(string username) =>
        username.Trim().ToUpperInvariant();

    public static string NormalizeEmail(string email) =>
        email.Trim().ToUpperInvariant();
}
