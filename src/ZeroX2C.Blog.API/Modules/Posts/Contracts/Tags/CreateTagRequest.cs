using System.ComponentModel.DataAnnotations;
using ZeroX2C.Blog.API.Modules.Posts.Tags;

namespace ZeroX2C.Blog.API.Modules.Posts.Contracts.Tags;

public sealed record CreateTagRequest(
    [Required]
    [MaxLength(TagName.MaxLength)]
    [RegularExpression(TagName.Pattern)]
    string Name,
    [MaxLength(512)] string? Description
);
