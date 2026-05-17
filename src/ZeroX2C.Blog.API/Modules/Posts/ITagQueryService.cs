using ZeroX2C.Blog.API.Modules.Posts.Contracts.Tags;

namespace ZeroX2C.Blog.API.Modules.Posts;

public interface ITagQueryService
{
    Task<IReadOnlyCollection<TagResponse>> GetTagsAsync(
        int offset,
        int limit,
        string? search,
        CancellationToken cancellationToken
    );
}
