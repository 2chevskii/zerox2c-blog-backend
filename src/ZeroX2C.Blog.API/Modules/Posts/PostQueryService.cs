using Microsoft.EntityFrameworkCore;
using ZeroX2C.Blog.API.Modules.Posts.Contracts;
using ZeroX2C.Blog.API.Persistence;

namespace ZeroX2C.Blog.API.Modules.Posts;

public sealed class PostQueryService(BlogDbContext dbContext) : IPostQueryService
{
    public async Task<IReadOnlyCollection<PostListItemResponse>> GetPublishedPostsAsync(
        int offset,
        int limit,
        string? search,
        CancellationToken cancellationToken
    )
    {
        var query = PublishedPostsQuery();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();
            query = query.Where(post =>
                post.Title.Contains(normalizedSearch)
                || (post.Subtitle != null && post.Subtitle.Contains(normalizedSearch))
                || (post.Excerpt != null && post.Excerpt.Contains(normalizedSearch))
            );
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
        var post = await PublishedPostsQuery()
            .SingleOrDefaultAsync(existingPost => existingPost.Id == id, cancellationToken);

        return post is null ? null : PostMapper.ToDetailsResponse(post);
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

        var post = await PublishedPostsQuery()
            .SingleOrDefaultAsync(
                existingPost => existingPost.Slug == normalizedSlug,
                cancellationToken
            );

        return post is null ? null : PostMapper.ToDetailsResponse(post);
    }

    private IQueryable<Post> PublishedPostsQuery() =>
        dbContext
            .Posts.Include(post => post.PostTags.Where(postTag => !postTag.IsDeleted))
            .ThenInclude(postTag => postTag.Tag)
            .Where(post =>
                !post.IsDeleted
                && post.Status == PostStatus.Published
                && post.PublishedAt != null
            );
}
