namespace ZeroX2C.Blog.API.Modules.Posts.Admin;

public sealed record SlugOperationResult(AdminPostOperationStatus Status, string? Value = null)
{
    public static SlugOperationResult Success(string? value) =>
        new(AdminPostOperationStatus.Success, value);

    public static SlugOperationResult Failure(AdminPostOperationStatus status) => new(status);
}
