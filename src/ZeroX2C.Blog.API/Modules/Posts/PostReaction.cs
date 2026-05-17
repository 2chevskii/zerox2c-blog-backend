using ZeroX2C.Blog.API.Modules.Shared;
using ZeroX2C.Blog.API.Modules.Users;

namespace ZeroX2C.Blog.API.Modules.Posts;

public sealed class PostReaction : EntityBase
{
    public required Guid PostId { get; set; }
    public Post Post { get; set; } = null!;
    public required Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public required PostReactionType ReactionType { get; set; }
}
