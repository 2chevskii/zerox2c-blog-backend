namespace ZeroX2C.Blog.API.Modules.Users.Auth;

public interface IPasswordHasher
{
    byte[] HashPassword(string password);
    bool VerifyPassword(string password, byte[] passwordHash);
}
