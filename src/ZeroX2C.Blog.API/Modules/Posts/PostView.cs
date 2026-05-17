using ZeroX2C.Blog.API.Modules.Shared;
using ZeroX2C.Blog.API.Modules.Users;

namespace ZeroX2C.Blog.API.Modules.Posts;

public sealed class PostView : EntityBase
{
    public required Guid PostId { get; set; }
    public Post Post { get; set; } = null!;
    public required Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public long ViewCount { get; set; }
    public DateTime LastViewedAt { get; set; }
}
