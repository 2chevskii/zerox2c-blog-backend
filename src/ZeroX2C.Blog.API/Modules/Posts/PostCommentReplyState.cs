using ZeroX2C.Blog.API.Modules.Shared;
using ZeroX2C.Blog.API.Modules.Users;

namespace ZeroX2C.Blog.API.Modules.Posts;

public sealed class PostCommentReplyState : EntityBase
{
    public required Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public required Guid ReplyCommentId { get; set; }
    public PostComment ReplyComment { get; set; } = null!;
    public DateTime? SeenAt { get; set; }
}
