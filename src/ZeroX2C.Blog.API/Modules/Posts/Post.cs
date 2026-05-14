using ZeroX2C.Blog.API.Modules.Shared;

namespace ZeroX2C.Blog.API.Modules.Posts;

public class Post : EntityBase
{
    public string? Slug { get; set; }
    public required string Title { get; set; }
    public string? Subtitle { get; set; }
    public required string Body { get; set; }
    public Guid? CoverImageId { get; set; }
    public Guid? BannerImageId { get; set; }
}
