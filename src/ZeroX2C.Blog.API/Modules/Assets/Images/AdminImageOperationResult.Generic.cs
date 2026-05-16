namespace ZeroX2C.Blog.API.Modules.Assets.Images;

public sealed record AdminImageOperationResult<T>(AdminImageOperationStatus Status, T? Value = default)
{
    public static AdminImageOperationResult<T> Success(T value) =>
        new(AdminImageOperationStatus.Success, value);

    public static AdminImageOperationResult<T> Failure(AdminImageOperationStatus status) =>
        new(status);
}
