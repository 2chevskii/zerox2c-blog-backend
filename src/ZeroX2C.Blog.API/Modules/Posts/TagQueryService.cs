using Microsoft.EntityFrameworkCore;
using ZeroX2C.Blog.API.Modules.Posts.Contracts.Tags;
using ZeroX2C.Blog.API.Modules.Posts.Tags;
using ZeroX2C.Blog.API.Persistence;

namespace ZeroX2C.Blog.API.Modules.Posts;

public sealed class TagQueryService(BlogDbContext dbContext) : ITagQueryService
{
    public async Task<IReadOnlyCollection<TagResponse>> GetTagsAsync(
        int offset,
        int limit,
        string? search,
        CancellationToken cancellationToken
    )
    {
        var query = dbContext.Tags.Where(tag => !tag.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();
            query = query.Where(tag =>
                tag.Name.Contains(normalizedSearch)
                || (tag.Description != null && tag.Description.Contains(normalizedSearch))
            );
        }

        var tags = await query
            .OrderBy(tag => tag.Name)
            .ThenBy(tag => tag.Id)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return tags.Select(TagMapper.ToResponse).ToArray();
    }
}
