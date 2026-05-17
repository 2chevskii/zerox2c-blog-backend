using ZeroX2C.Blog.API.Modules.Shared;
using ZeroX2C.Blog.API.Modules.Posts.Markdown;
using ZeroX2C.Blog.API.Modules.Posts.Tags;

namespace ZeroX2C.Blog.API.Modules.Posts;

public class Post : EntityBase
{
    public string? Slug { get; set; }
    public required string Title { get; set; }
    public string? Subtitle { get; set; }
    public PostStatus Status { get; set; } = PostStatus.Draft;
    public long LikeCount { get; set; }
    public long DislikeCount { get; set; }
    public long CommentCount { get; set; }
    public long ViewCount { get; set; }
    public Guid? PublishedBy { get; set; }
    public DateTime? PublishedAt { get; set; }
    public Guid? CoverImageId { get; set; }
    public Guid? BannerImageId { get; set; }

    public List<PostTag> PostTags { get; set; } = [];
    public List<PostReaction> Reactions { get; set; } = [];
    public List<PostComment> Comments { get; set; } = [];
    public PostMarkdownDraft? MarkdownDraft { get; set; }
    public PostMarkdownDocument? MarkdownDocument { get; set; }
    public List<PostMarkdownImage> MarkdownImages { get; set; } = [];
}
