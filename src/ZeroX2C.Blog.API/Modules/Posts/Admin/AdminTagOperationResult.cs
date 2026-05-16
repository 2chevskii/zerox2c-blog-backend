namespace ZeroX2C.Blog.API.Modules.Posts.Admin;

public sealed record AdminTagOperationResult(AdminTagOperationStatus Status)
{
    public static AdminTagOperationResult Success() =>
        new(AdminTagOperationStatus.Success);

    public static AdminTagOperationResult Failure(AdminTagOperationStatus status) =>
        new(status);
}
