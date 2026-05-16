namespace ZeroX2C.Blog.API.Modules.Posts.Admin;

public sealed record AdminPostOperationResult<T>(AdminPostOperationStatus Status, T? Value = default)
{
    public static AdminPostOperationResult<T> Success(T value) =>
        new(AdminPostOperationStatus.Success, value);

    public static AdminPostOperationResult<T> Failure(AdminPostOperationStatus status) =>
        new(status);
}
