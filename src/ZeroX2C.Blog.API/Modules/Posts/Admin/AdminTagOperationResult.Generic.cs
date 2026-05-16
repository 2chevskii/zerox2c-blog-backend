namespace ZeroX2C.Blog.API.Modules.Posts.Admin;

public sealed record AdminTagOperationResult<T>(AdminTagOperationStatus Status, T? Value = default)
{
    public static AdminTagOperationResult<T> Success(T value) =>
        new(AdminTagOperationStatus.Success, value);

    public static AdminTagOperationResult<T> Failure(AdminTagOperationStatus status) =>
        new(status);
}
