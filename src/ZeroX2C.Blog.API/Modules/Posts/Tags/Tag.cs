using ZeroX2C.Blog.API.Modules.Shared;

namespace ZeroX2C.Blog.API.Modules.Posts.Tags;

public sealed class Tag : EntityBase
{
    public required string Name { get; set; }
    public string? Description { get; set; }

    public List<PostTag> PostTags { get; set; } = [];
}
