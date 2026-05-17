using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ZeroX2C.Blog.API.Modules.Posts.Contracts;
using ZeroX2C.Blog.API.Modules.Users.Auth;
using ZeroX2C.Blog.API.Persistence;

namespace ZeroX2C.Blog.API.Modules.Posts;

public sealed class PostReactionService(
    BlogDbContext dbContext,
    IAuthenticationContext authenticationContext,
    IMemoryCache memoryCache
) : IPostReactionService
{
    public async Task<PostReactionResponse?> GetReactionAsync(
        Guid postId,
        CancellationToken cancellationToken
    )
    {
        var post = await GetPublishedPostAsync(postId, cancellationToken);
        if (post is null)
        {
            return null;
        }

        var reaction = await GetUserReactionAsync(postId, cancellationToken);
        return ToResponse(post, reaction);
    }

    public async Task<PostReactionOperationResult> SetReactionAsync(
        Guid postId,
        PostReactionType reactionType,
        CancellationToken cancellationToken
    )
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken
        );

        var post = await GetPublishedPostAsync(postId, cancellationToken);
        if (post is null)
        {
            return PostReactionOperationResult.PostNotFound();
        }

        var reaction = await GetUserReactionAsync(postId, cancellationToken);
        var previousReactionType = GetActiveReactionType(reaction);

        if (reaction is null)
        {
            reaction = new PostReaction
            {
                Id = Guid.CreateVersion7(),
                PostId = postId,
                UserId = authenticationContext.UserId,
                ReactionType = reactionType,
            };
            dbContext.PostReactions.Add(reaction);
        }
        else
        {
            reaction.IsDeleted = false;
            reaction.ReactionType = reactionType;
        }

        ApplyCounterDelta(post, previousReactionType, reactionType);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        RemoveCachedPost(postId);
        return PostReactionOperationResult.Success(ToResponse(post, reaction));
    }

    public async Task<PostReactionOperationResult> ClearReactionAsync(
        Guid postId,
        CancellationToken cancellationToken
    )
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken
        );

        var post = await GetPublishedPostAsync(postId, cancellationToken);
        if (post is null)
        {
            return PostReactionOperationResult.PostNotFound();
        }

        var reaction = await GetUserReactionAsync(postId, cancellationToken);
        var previousReactionType = GetActiveReactionType(reaction);

        if (previousReactionType is not null && reaction is not null)
        {
            ApplyCounterDelta(post, previousReactionType, null);
            dbContext.PostReactions.Remove(reaction);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        RemoveCachedPost(postId);
        return PostReactionOperationResult.Success(ToResponse(post, null));
    }

    private Task<Post?> GetPublishedPostAsync(Guid postId, CancellationToken cancellationToken) =>
        dbContext.Posts.SingleOrDefaultAsync(
            post =>
                post.Id == postId
                && !post.IsDeleted
                && post.Status == PostStatus.Published
                && post.PublishedAt != null
                && post.MarkdownDocument != null,
            cancellationToken
        );

    private Task<PostReaction?> GetUserReactionAsync(
        Guid postId,
        CancellationToken cancellationToken
    ) =>
        dbContext.PostReactions.SingleOrDefaultAsync(
            reaction =>
                reaction.PostId == postId && reaction.UserId == authenticationContext.UserId,
            cancellationToken
        );

    private static PostReactionType? GetActiveReactionType(PostReaction? reaction) =>
        reaction is { IsDeleted: false } ? reaction.ReactionType : null;

    private static void ApplyCounterDelta(
        Post post,
        PostReactionType? previousReactionType,
        PostReactionType? nextReactionType
    )
    {
        if (previousReactionType == nextReactionType)
        {
            return;
        }

        if (previousReactionType == PostReactionType.Like)
        {
            post.LikeCount = Math.Max(0, post.LikeCount - 1);
        }
        else if (previousReactionType == PostReactionType.Dislike)
        {
            post.DislikeCount = Math.Max(0, post.DislikeCount - 1);
        }

        if (nextReactionType == PostReactionType.Like)
        {
            post.LikeCount++;
        }
        else if (nextReactionType == PostReactionType.Dislike)
        {
            post.DislikeCount++;
        }
    }

    private static PostReactionResponse ToResponse(Post post, PostReaction? reaction) =>
        new(post.Id, post.LikeCount, post.DislikeCount, GetActiveReactionType(reaction));

    private void RemoveCachedPost(Guid postId) => memoryCache.Remove($"published-post:id:{postId:N}");
}
