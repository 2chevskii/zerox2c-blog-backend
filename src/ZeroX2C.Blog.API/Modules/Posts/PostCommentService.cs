using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ZeroX2C.Blog.API.Modules.Posts.Contracts;
using ZeroX2C.Blog.API.Modules.Users;
using ZeroX2C.Blog.API.Modules.Users.Auth;
using ZeroX2C.Blog.API.Persistence;

namespace ZeroX2C.Blog.API.Modules.Posts;

public sealed class PostCommentService(
    BlogDbContext dbContext,
    IAuthenticationContext authenticationContext,
    IMemoryCache memoryCache
) : IPostCommentService
{
    public async Task<IReadOnlyCollection<PostCommentResponse>?> GetVisibleCommentsAsync(
        Guid postId,
        CancellationToken cancellationToken
    )
    {
        var postExists = await PublishedPostExistsAsync(postId, cancellationToken);
        if (!postExists)
        {
            return null;
        }

        var comments = await dbContext.PostComments
            .Include(comment => comment.AuthorUser)
            .Where(comment =>
                comment.PostId == postId
                && !comment.IsDeleted
                && comment.Status == PostCommentStatus.Visible
            )
            .OrderBy(comment => comment.CreatedAt)
            .ThenBy(comment => comment.Id)
            .ToListAsync(cancellationToken);

        return comments.Select(ToResponse).ToArray();
    }

    public async Task<PostCommentOperationResult> CreateCommentAsync(
        Guid postId,
        string body,
        Guid? parentCommentId,
        CancellationToken cancellationToken
    )
    {
        var normalizedBody = NormalizeBody(body);
        if (normalizedBody is null)
        {
            return PostCommentOperationResult.Failure(PostCommentOperationStatus.EmptyBody);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken
        );

        var post = await GetPublishedPostAsync(postId, cancellationToken);
        if (post is null)
        {
            return PostCommentOperationResult.Failure(PostCommentOperationStatus.PostNotFound);
        }

        if (parentCommentId is not null)
        {
            var parentExists = await dbContext.PostComments.AnyAsync(
                comment =>
                    comment.Id == parentCommentId
                    && comment.PostId == postId
                    && !comment.IsDeleted
                    && comment.Status == PostCommentStatus.Visible,
                cancellationToken
            );

            if (!parentExists)
            {
                return PostCommentOperationResult.Failure(
                    PostCommentOperationStatus.ParentCommentNotFound
                );
            }
        }

        var comment = new PostComment
        {
            Id = Guid.CreateVersion7(),
            PostId = postId,
            AuthorUserId = authenticationContext.UserId,
            ParentCommentId = parentCommentId,
            Body = normalizedBody,
        };

        post.CommentCount++;
        dbContext.PostComments.Add(comment);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        RemoveCachedPost(postId);

        await dbContext.Entry(comment).Reference(value => value.AuthorUser).LoadAsync(cancellationToken);
        return PostCommentOperationResult.Success(ToResponse(comment));
    }

    public async Task<PostCommentOperationResult> UpdateCommentAsync(
        Guid postId,
        Guid commentId,
        string body,
        CancellationToken cancellationToken
    )
    {
        var normalizedBody = NormalizeBody(body);
        if (normalizedBody is null)
        {
            return PostCommentOperationResult.Failure(PostCommentOperationStatus.EmptyBody);
        }

        var postExists = await PublishedPostExistsAsync(postId, cancellationToken);
        if (!postExists)
        {
            return PostCommentOperationResult.Failure(PostCommentOperationStatus.PostNotFound);
        }

        var comment = await dbContext.PostComments
            .Include(value => value.AuthorUser)
            .SingleOrDefaultAsync(
                value =>
                    value.Id == commentId
                    && value.PostId == postId
                    && !value.IsDeleted
                    && value.Status == PostCommentStatus.Visible,
                cancellationToken
            );

        if (comment is null)
        {
            return PostCommentOperationResult.Failure(PostCommentOperationStatus.CommentNotFound);
        }

        if (!CanEdit(comment))
        {
            return PostCommentOperationResult.Failure(PostCommentOperationStatus.NotCommentAuthor);
        }

        comment.Body = normalizedBody;
        await dbContext.SaveChangesAsync(cancellationToken);

        return PostCommentOperationResult.Success(ToResponse(comment));
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

    private async Task<bool> PublishedPostExistsAsync(
        Guid postId,
        CancellationToken cancellationToken
    ) => await GetPublishedPostAsync(postId, cancellationToken) is not null;

    private bool CanEdit(PostComment comment) =>
        comment.AuthorUserId == authenticationContext.UserId
        || authenticationContext.Role is UserRole.Admin or UserRole.SuperAdmin;

    private static string? NormalizeBody(string body)
    {
        var normalized = body.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static PostCommentResponse ToResponse(PostComment comment) =>
        new(
            comment.Id,
            comment.PostId,
            comment.AuthorUserId,
            comment.AuthorUser.Username,
            comment.ParentCommentId,
            comment.Body,
            comment.CreatedAt,
            comment.UpdatedAt
        );

    private void RemoveCachedPost(Guid postId) => memoryCache.Remove($"published-post:id:{postId:N}");
}
