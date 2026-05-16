using ZeroX2C.Blog.API.Modules.Shared;
using ZeroX2C.Blog.API.Modules.Posts.Tags;

namespace ZeroX2C.Blog.API.Modules.Posts;

public class Post : EntityBase
{
    public string? Slug { get; set; }
    public required string Title { get; set; }
    public string? Subtitle { get; set; }
    public string? Excerpt { get; set; }
    public required string Body { get; set; }
    public PostStatus Status { get; set; } = PostStatus.Draft;
    public Guid? PublishedBy { get; set; }
    public DateTime? PublishedAt { get; set; }
    public Guid? CoverImageId { get; set; }
    public Guid? BannerImageId { get; set; }

    public List<PostTag> PostTags { get; set; } = [];
}
