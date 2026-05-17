using System.ComponentModel.DataAnnotations;

namespace ZeroX2C.Blog.API.Modules.Posts.Contracts;

public sealed record CreatePostRequest(
    [MaxLength(PostSlug.MaxLength)]
    [RegularExpression(PostSlug.Pattern)]
    string? Slug,
    [Required, MaxLength(256)] string Title,
    [MaxLength(512)] string? Subtitle,
    [Required] string BodyMarkdown,
    Guid? CoverImageId,
    Guid? BannerImageId,
    IReadOnlyCollection<Guid>? TagIds
);
