namespace ZeroX2C.Blog.API.Modules.Posts.Admin;

public sealed record TagNameOperationResult(AdminTagOperationStatus Status, string? Value = null)
{
    public static TagNameOperationResult Success(string value) =>
        new(AdminTagOperationStatus.Success, value);

    public static TagNameOperationResult Failure(AdminTagOperationStatus status) => new(status);
}
