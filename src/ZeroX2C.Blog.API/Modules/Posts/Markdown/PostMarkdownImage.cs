using ZeroX2C.Blog.API.Modules.Assets.Images;
using ZeroX2C.Blog.API.Modules.Shared;

namespace ZeroX2C.Blog.API.Modules.Posts.Markdown;

public class PostMarkdownImage : EntityBase
{
    public const int LocalPathMaxLength = 128;

    public Guid PostId { get; set; }
    public Post Post { get; set; } = null!;
    public Guid ImageId { get; set; }
    public Image Image { get; set; } = null!;
    public required string LocalPath { get; set; }
}
