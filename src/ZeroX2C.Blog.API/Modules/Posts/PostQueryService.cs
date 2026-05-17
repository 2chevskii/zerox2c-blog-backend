using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ZeroX2C.Blog.API.Modules.Posts.Contracts;
using ZeroX2C.Blog.API.Modules.Posts.Tags;
using ZeroX2C.Blog.API.Modules.Users.Auth;
using ZeroX2C.Blog.API.Persistence;

namespace ZeroX2C.Blog.API.Modules.Posts;

public sealed class PostQueryService(
    BlogDbContext dbContext,
    IMemoryCache memoryCache,
    IAuthenticationContext authenticationContext,
    TimeProvider timeProvider
)
    : IPostQueryService
{
    private static readonly TimeSpan PostDetailsCacheLifetime = TimeSpan.FromMinutes(5);

    public async Task<IReadOnlyCollection<PostListItemResponse>> GetPublishedPostsAsync(
        int offset,
        int limit,
        string? search,
        string? tags,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken
    )
    {
        var query = PublishedPostsQuery();
        var tagNames = NormalizeTagNames(tags);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();
            query = query.Where(post =>
                post.Title.Contains(normalizedSearch)
                || (post.Subtitle != null && post.Subtitle.Contains(normalizedSearch))
                || post.PostTags.Any(postTag =>
                    !postTag.IsDeleted
                    && !postTag.Tag.IsDeleted
                    && postTag.Tag.Name.Contains(normalizedSearch)
                )
            );
        }

        if (tagNames.Length > 0)
        {
            query = query.Where(post =>
                post.PostTags
                    .Where(postTag =>
                        !postTag.IsDeleted
                        && !postTag.Tag.IsDeleted
                        && tagNames.Contains(postTag.Tag.Name)
                    )
                    .Select(postTag => postTag.Tag.Name)
                    .Distinct()
                    .Count()
                == tagNames.Length
            );
        }

        if (from is not null)
        {
            var publishedFrom = from.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(post => post.PublishedAt >= publishedFrom);
        }

        if (to is not null)
        {
            var publishedBefore = to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            query = query.Where(post => post.PublishedAt < publishedBefore);
        }

        var posts = await query
            .OrderByDescending(post => post.PublishedAt)
            .ThenByDescending(post => post.Id)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return posts.Select(PostMapper.ToListItemResponse).ToArray();
    }

    public async Task<PostDetailsResponse?> GetPublishedPostByIdAsync(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        var cacheKey = $"published-post:id:{id:N}";

        if (
            memoryCache.TryGetValue<PostDetailsResponse>(cacheKey, out var cachedPost)
            && cachedPost is not null
        )
        {
            return await IncrementViewCountAsync(cachedPost, cancellationToken);
        }

        var post = await PublishedPostsQuery()
            .SingleOrDefaultAsync(existingPost => existingPost.Id == id, cancellationToken);
        if (post is null)
        {
            return null;
        }

        var response = await IncrementViewCountAsync(
            PostMapper.ToDetailsResponse(post),
            cancellationToken
        );

        return response;
    }

    private async Task<PostDetailsResponse> IncrementViewCountAsync(
        PostDetailsResponse post,
        CancellationToken cancellationToken
    )
    {
        await dbContext.Posts
            .Where(existingPost => existingPost.Id == post.Id && !existingPost.IsDeleted)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    existingPost => existingPost.ViewCount,
                    existingPost => existingPost.ViewCount + 1
                ),
                cancellationToken
            );

        await TrackAuthenticatedPostViewAsync(post.Id, cancellationToken);

        var updatedPost = post with { ViewCount = post.ViewCount + 1 };
        memoryCache.Set(
            $"published-post:id:{post.Id:N}",
            updatedPost,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = PostDetailsCacheLifetime,
            }
        );

        return updatedPost;
    }

    private async Task TrackAuthenticatedPostViewAsync(
        Guid postId,
        CancellationToken cancellationToken
    )
    {
        if (!authenticationContext.IsAuthenticated)
        {
            return;
        }

        var now = timeProvider.GetUtcNow().DateTime;
        var postView = await dbContext.PostViews.SingleOrDefaultAsync(
            view => view.PostId == postId && view.UserId == authenticationContext.UserId,
            cancellationToken
        );

        if (postView is null)
        {
            dbContext.PostViews.Add(
                new PostView
                {
                    Id = Guid.CreateVersion7(),
                    PostId = postId,
                    UserId = authenticationContext.UserId,
                    ViewCount = 1,
                    LastViewedAt = now,
                }
            );
        }
        else
        {
            postView.ViewCount++;
            postView.LastViewedAt = now;
            postView.IsDeleted = false;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PostDetailsResponse?> GetPublishedPostBySlugAsync(
        string slug,
        CancellationToken cancellationToken
    )
    {
        var normalizedSlug = PostSlug.Normalize(slug);
        if (normalizedSlug is null)
        {
            return null;
        }

        var postId = await PublishedPostsQuery()
            .Where(existingPost => existingPost.Slug == normalizedSlug)
            .Select(existingPost => (Guid?)existingPost.Id)
            .SingleOrDefaultAsync(cancellationToken);

        return postId is null
            ? null
            : await GetPublishedPostByIdAsync(postId.Value, cancellationToken);
    }

    private IQueryable<Post> PublishedPostsQuery() =>
        dbContext
            .Posts.Include(post => post.PostTags.Where(postTag => !postTag.IsDeleted))
            .ThenInclude(postTag => postTag.Tag)
            .Include(post => post.MarkdownDocument)
            .Where(post =>
                !post.IsDeleted
                && post.Status == PostStatus.Published
                && post.PublishedAt != null
                && post.MarkdownDocument != null
            );

    private static string[] NormalizeTagNames(string? tags) =>
        (tags ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(TagName.Normalize)
            .Where(tagName => !string.IsNullOrWhiteSpace(tagName) && TagName.IsValid(tagName))
            .Distinct(StringComparer.Ordinal)
            .ToArray()!;
}
