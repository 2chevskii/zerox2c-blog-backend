using ZeroX2C.Blog.API.Modules.Posts.Tags;

namespace ZeroX2C.Blog.API.Modules.Posts.Admin;

public sealed record TagCollectionOperationResult(
    AdminPostOperationStatus Status,
    IReadOnlyCollection<Tag>? Value = null
)
{
    public static TagCollectionOperationResult Success(IReadOnlyCollection<Tag> value) =>
        new(AdminPostOperationStatus.Success, value);

    public static TagCollectionOperationResult Failure(AdminPostOperationStatus status) =>
        new(status);
}
