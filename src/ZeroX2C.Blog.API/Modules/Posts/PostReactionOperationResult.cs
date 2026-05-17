using ZeroX2C.Blog.API.Modules.Posts.Contracts;

namespace ZeroX2C.Blog.API.Modules.Posts;

public sealed record PostReactionOperationResult(
    PostReactionOperationStatus Status,
    PostReactionResponse? Response = null
)
{
    public static PostReactionOperationResult Success(PostReactionResponse response) =>
        new(PostReactionOperationStatus.Success, response);

    public static PostReactionOperationResult PostNotFound() =>
        new(PostReactionOperationStatus.PostNotFound);
}
