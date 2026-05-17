using ZeroX2C.Blog.API.Modules.Posts.Contracts;

namespace ZeroX2C.Blog.API.Modules.Posts;

public sealed record PostCommentOperationResult(
    PostCommentOperationStatus Status,
    PostCommentResponse? Response = null
)
{
    public static PostCommentOperationResult Success(PostCommentResponse response) =>
        new(PostCommentOperationStatus.Success, response);

    public static PostCommentOperationResult Failure(PostCommentOperationStatus status) =>
        new(status);
}
