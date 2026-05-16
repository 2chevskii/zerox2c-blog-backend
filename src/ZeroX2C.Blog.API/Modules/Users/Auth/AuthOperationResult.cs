namespace ZeroX2C.Blog.API.Modules.Users.Auth;

public sealed record AuthOperationResult<T>(AuthOperationStatus Status, T? Value = default)
{
    public static AuthOperationResult<T> Success(T value) =>
        new(AuthOperationStatus.Success, value);

    public static AuthOperationResult<T> Failure(AuthOperationStatus status) => new(status);
}
