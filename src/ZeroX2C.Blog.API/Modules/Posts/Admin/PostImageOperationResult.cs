namespace ZeroX2C.Blog.API.Modules.Posts.Admin;

public sealed record PostImageOperationResult(AdminPostOperationStatus Status)
{
    public static PostImageOperationResult Success() =>
        new(AdminPostOperationStatus.Success);

    public static PostImageOperationResult Failure(AdminPostOperationStatus status) =>
        new(status);
}
