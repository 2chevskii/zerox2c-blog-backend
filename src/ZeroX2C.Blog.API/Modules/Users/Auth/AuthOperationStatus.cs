namespace ZeroX2C.Blog.API.Modules.Users.Auth;

public enum AuthOperationStatus
{
    Success,
    UsernameAlreadyTaken,
    EmailAlreadyTaken,
    InvalidCredentials,
    ExternalLoginFailed,
    UserBlocked,
    UserNotFound,
}
