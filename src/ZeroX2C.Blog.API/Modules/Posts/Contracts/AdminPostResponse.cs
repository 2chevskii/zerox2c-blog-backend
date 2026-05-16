using ZeroX2C.Blog.API.Modules.Posts.Contracts.Tags;

namespace ZeroX2C.Blog.API.Modules.Posts.Contracts;

public sealed record AdminPostResponse(
    Guid Id,
    string? Slug,
    string Title,
    string? Subtitle,
    string? Excerpt,
    string Body,
    PostStatus Status,
    Guid? CoverImageId,
    Guid? BannerImageId,
    IReadOnlyCollection<TagResponse> Tags,
    Guid CreatedBy,
    DateTime CreatedAt,
    Guid? UpdatedBy,
    DateTime? UpdatedAt,
    Guid? PublishedBy,
    DateTime? PublishedAt,
    Guid? DeletedBy,
    DateTime? DeletedAt
);
