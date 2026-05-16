using ZeroX2C.Blog.API.Modules.Posts.Contracts.Tags;

namespace ZeroX2C.Blog.API.Modules.Posts.Contracts;

public sealed record PostDetailsResponse(
    Guid Id,
    string? Slug,
    string Title,
    string? Subtitle,
    string? Excerpt,
    string Body,
    Guid? CoverImageId,
    Guid? BannerImageId,
    IReadOnlyCollection<TagResponse> Tags,
    DateTime? PublishedAt
);
