using ZeroX2C.Blog.API.Modules.Shared;

namespace ZeroX2C.Blog.API.Modules.Posts.Tags;

public sealed class PostTag : EntityBase
{
    public required Guid PostId { get; set; }
    public Post Post { get; set; } = null!;

    public required Guid TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
