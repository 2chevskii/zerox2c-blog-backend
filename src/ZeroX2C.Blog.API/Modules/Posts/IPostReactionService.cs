using ZeroX2C.Blog.API.Modules.Posts.Contracts;

namespace ZeroX2C.Blog.API.Modules.Posts;

public interface IPostReactionService
{
    Task<PostReactionResponse?> GetReactionAsync(Guid postId, CancellationToken cancellationToken);

    Task<PostReactionOperationResult> SetReactionAsync(
        Guid postId,
        PostReactionType reactionType,
        CancellationToken cancellationToken
    );

    Task<PostReactionOperationResult> ClearReactionAsync(
        Guid postId,
        CancellationToken cancellationToken
    );
}
