using System.ComponentModel.DataAnnotations;

namespace ZeroX2C.Blog.API.Modules.Posts.Contracts;

public sealed record UpdatePostRequest(
    [MaxLength(PostSlug.MaxLength)]
    [RegularExpression(PostSlug.Pattern)]
    string? Slug,
    [Required, MaxLength(256)] string Title,
    [MaxLength(512)] string? Subtitle,
    [MaxLength(1000)] string? Excerpt,
    [Required] string Body,
    Guid? CoverImageId,
    Guid? BannerImageId,
    IReadOnlyCollection<Guid>? TagIds
);
