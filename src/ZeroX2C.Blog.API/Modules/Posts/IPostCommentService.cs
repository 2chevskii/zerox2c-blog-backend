using ZeroX2C.Blog.API.Modules.Posts.Contracts;

namespace ZeroX2C.Blog.API.Modules.Posts;

public interface IPostCommentService
{
    Task<IReadOnlyCollection<PostCommentResponse>?> GetVisibleCommentsAsync(
        Guid postId,
        CancellationToken cancellationToken
    );

    Task<PostCommentOperationResult> CreateCommentAsync(
        Guid postId,
        string body,
        Guid? parentCommentId,
        CancellationToken cancellationToken
    );

    Task<PostCommentOperationResult> UpdateCommentAsync(
        Guid postId,
        Guid commentId,
        string body,
        CancellationToken cancellationToken
    );
}
