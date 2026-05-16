namespace ZeroX2C.Blog.API.Modules.Posts.Admin;

public sealed record AdminPostOperationResult(AdminPostOperationStatus Status)
{
    public static AdminPostOperationResult Success() =>
        new(AdminPostOperationStatus.Success);

    public static AdminPostOperationResult Failure(AdminPostOperationStatus status) =>
        new(status);
}
