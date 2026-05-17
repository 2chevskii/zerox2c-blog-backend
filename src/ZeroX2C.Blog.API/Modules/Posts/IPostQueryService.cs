using ZeroX2C.Blog.API.Modules.Posts.Contracts;

namespace ZeroX2C.Blog.API.Modules.Posts;

public interface IPostQueryService
{
    Task<IReadOnlyCollection<PostListItemResponse>> GetPublishedPostsAsync(
        int offset,
        int limit,
        string? search,
        string? tags,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken
    );

    Task<PostDetailsResponse?> GetPublishedPostByIdAsync(
        Guid id,
        CancellationToken cancellationToken
    );

    Task<PostDetailsResponse?> GetPublishedPostBySlugAsync(
        string slug,
        CancellationToken cancellationToken
    );
}
