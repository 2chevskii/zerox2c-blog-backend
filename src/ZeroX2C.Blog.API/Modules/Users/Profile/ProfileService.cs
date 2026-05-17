using Microsoft.EntityFrameworkCore;
using ZeroX2C.Blog.API.Modules.Assets.Images;
using ZeroX2C.Blog.API.Modules.Posts;
using ZeroX2C.Blog.API.Modules.Users.Auth;
using ZeroX2C.Blog.API.Modules.Users.Contracts.Profile;
using ZeroX2C.Blog.API.Persistence;

namespace ZeroX2C.Blog.API.Modules.Users.Profile;

public sealed class ProfileService(
    BlogDbContext dbContext,
    IAuthenticationContext authenticationContext,
    TimeProvider timeProvider
) : IProfileService
{
    private const int ProfileListLimit = 20;
    private const long MaxAvatarSizeBytes = 2 * 1024 * 1024;

    private static readonly HashSet<string> AllowedAvatarContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp",
    ];

    public async Task<ProfileResponse?> GetProfileAsync(CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                existingUser =>
                    existingUser.Id == authenticationContext.UserId && !existingUser.IsDeleted,
                cancellationToken
            );

        return user is null ? null : await BuildProfileResponseAsync(user, cancellationToken);
    }

    public async Task<ProfileOperationResult<ProfileResponse>> UploadAvatarAsync(
        UploadAvatarRequest request,
        CancellationToken cancellationToken
    )
    {
        var file = request.File;
        if (file.Length == 0)
        {
            return ProfileOperationResult<ProfileResponse>.Failure(ProfileOperationStatus.EmptyFile);
        }

        if (file.Length > MaxAvatarSizeBytes)
        {
            return ProfileOperationResult<ProfileResponse>.Failure(
                ProfileOperationStatus.FileTooLarge
            );
        }

        var contentType = file.ContentType.Trim().ToLowerInvariant();
        if (!AllowedAvatarContentTypes.Contains(contentType))
        {
            return ProfileOperationResult<ProfileResponse>.Failure(
                ProfileOperationStatus.UnsupportedContentType
            );
        }

        var user = await dbContext.Users.SingleOrDefaultAsync(
            existingUser => existingUser.Id == authenticationContext.UserId && !existingUser.IsDeleted,
            cancellationToken
        );
        if (user is null)
        {
            return ProfileOperationResult<ProfileResponse>.Failure(ProfileOperationStatus.UserNotFound);
        }

        await using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);

        var image = new Image
        {
            Id = Guid.CreateVersion7(),
            OriginalFileName = Path.GetFileName(file.FileName),
            ContentType = contentType,
            SizeBytes = memoryStream.Length,
            Purpose = ImagePurpose.Avatar,
            Content = memoryStream.ToArray(),
        };

        dbContext.Images.Add(image);
        user.AvatarImageId = image.Id;
        await dbContext.SaveChangesAsync(cancellationToken);

        return ProfileOperationResult<ProfileResponse>.Success(
            await BuildProfileResponseAsync(user, cancellationToken)
        );
    }

    public async Task<ProfileOperationResult<ProfileCommentReplyResponse>> MarkReplySeenAsync(
        Guid replyCommentId,
        CancellationToken cancellationToken
    )
    {
        var reply = await GetReplyQuery()
            .SingleOrDefaultAsync(comment => comment.Id == replyCommentId, cancellationToken);
        if (reply is null)
        {
            return ProfileOperationResult<ProfileCommentReplyResponse>.Failure(
                ProfileOperationStatus.ReplyNotFound
            );
        }

        var state = await dbContext.PostCommentReplyStates.SingleOrDefaultAsync(
            existingState =>
                existingState.UserId == authenticationContext.UserId
                && existingState.ReplyCommentId == replyCommentId,
            cancellationToken
        );

        var seenAt = timeProvider.GetUtcNow().DateTime;
        if (state is null)
        {
            state = new PostCommentReplyState
            {
                Id = Guid.CreateVersion7(),
                UserId = authenticationContext.UserId,
                ReplyCommentId = replyCommentId,
                SeenAt = seenAt,
            };
            dbContext.PostCommentReplyStates.Add(state);
        }
        else
        {
            state.SeenAt = seenAt;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return ProfileOperationResult<ProfileCommentReplyResponse>.Success(
            ToReplyResponse(reply, state)
        );
    }

    private async Task<ProfileResponse> BuildProfileResponseAsync(
        User user,
        CancellationToken cancellationToken
    )
    {
        var recentlyViewedPosts = await dbContext.PostViews
            .AsNoTracking()
            .Include(view => view.Post)
            .ThenInclude(post => post.PostTags.Where(postTag => !postTag.IsDeleted))
            .ThenInclude(postTag => postTag.Tag)
            .Where(view =>
                view.UserId == user.Id
                && !view.IsDeleted
                && !view.Post.IsDeleted
                && view.Post.Status == PostStatus.Published
                && view.Post.PublishedAt != null
            )
            .OrderByDescending(view => view.LastViewedAt)
            .Take(ProfileListLimit)
            .Select(view => view.Post)
            .ToListAsync(cancellationToken);

        var likedPosts = await dbContext.PostReactions
            .AsNoTracking()
            .Include(reaction => reaction.Post)
            .ThenInclude(post => post.PostTags.Where(postTag => !postTag.IsDeleted))
            .ThenInclude(postTag => postTag.Tag)
            .Where(reaction =>
                reaction.UserId == user.Id
                && !reaction.IsDeleted
                && reaction.ReactionType == PostReactionType.Like
                && !reaction.Post.IsDeleted
                && reaction.Post.Status == PostStatus.Published
                && reaction.Post.PublishedAt != null
            )
            .OrderByDescending(reaction => reaction.UpdatedAt ?? reaction.CreatedAt)
            .Take(ProfileListLimit)
            .Select(reaction => reaction.Post)
            .ToListAsync(cancellationToken);

        var comments = await dbContext.PostComments
            .AsNoTracking()
            .Include(comment => comment.Post)
            .Where(comment =>
                comment.AuthorUserId == user.Id
                && !comment.IsDeleted
                && comment.Status == PostCommentStatus.Visible
                && !comment.Post.IsDeleted
                && comment.Post.Status == PostStatus.Published
                && comment.Post.PublishedAt != null
            )
            .OrderByDescending(comment => comment.CreatedAt)
            .Take(ProfileListLimit)
            .Select(comment => ToProfileCommentResponse(comment))
            .ToListAsync(cancellationToken);

        var replies = await GetReplyQuery()
            .OrderByDescending(comment => comment.CreatedAt)
            .Take(ProfileListLimit)
            .ToListAsync(cancellationToken);
        var replyIds = replies.Select(reply => reply.Id).ToArray();
        var states = await dbContext.PostCommentReplyStates
            .AsNoTracking()
            .Where(state => state.UserId == user.Id && replyIds.Contains(state.ReplyCommentId))
            .ToDictionaryAsync(state => state.ReplyCommentId, cancellationToken);

        return new ProfileResponse(
            user.Id,
            user.Username,
            user.Email,
            user.AvatarImageId,
            recentlyViewedPosts.Select(PostMapper.ToListItemResponse).ToArray(),
            likedPosts.Select(PostMapper.ToListItemResponse).ToArray(),
            comments,
            replies.Select(reply =>
                ToReplyResponse(
                    reply,
                    states.GetValueOrDefault(reply.Id)
                )
            ).ToArray()
        );
    }

    private IQueryable<PostComment> GetReplyQuery() =>
        dbContext.PostComments
            .Include(comment => comment.AuthorUser)
            .Include(comment => comment.ParentComment)
            .Include(comment => comment.Post)
            .Where(comment =>
                comment.ParentCommentId != null
                && comment.ParentComment != null
                && comment.ParentComment.AuthorUserId == authenticationContext.UserId
                && comment.AuthorUserId != authenticationContext.UserId
                && !comment.IsDeleted
                && comment.Status == PostCommentStatus.Visible
                && !comment.ParentComment.IsDeleted
                && comment.ParentComment.Status == PostCommentStatus.Visible
                && !comment.Post.IsDeleted
                && comment.Post.Status == PostStatus.Published
                && comment.Post.PublishedAt != null
            );

    private static ProfileCommentResponse ToProfileCommentResponse(PostComment comment) =>
        new(
            comment.Id,
            comment.PostId,
            comment.Post.Slug,
            comment.Post.Title,
            comment.ParentCommentId,
            comment.Body,
            comment.CreatedAt,
            comment.UpdatedAt
        );

    private static ProfileCommentReplyResponse ToReplyResponse(
        PostComment reply,
        PostCommentReplyState? state
    ) =>
        new(
            reply.Id,
            reply.PostId,
            reply.Post.Slug,
            reply.Post.Title,
            reply.ParentCommentId!.Value,
            reply.ParentComment!.Body,
            reply.AuthorUserId,
            reply.AuthorUser.Username,
            reply.Body,
            reply.CreatedAt,
            state?.SeenAt is not null,
            state?.SeenAt
        );
}
