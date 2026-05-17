using ZeroX2C.Blog.API.Modules.Shared;

namespace ZeroX2C.Blog.API.Modules.Posts.Markdown;

public class PostMarkdownDraft : EntityBase
{
    public Guid PostId { get; set; }
    public Post Post { get; set; } = null!;
    public required MarkdownDocumentContent Document { get; set; }
}
