using ZeroX2C.Blog.API.Modules.Shared;
using ZeroX2C.Blog.API.Modules.Users;

namespace ZeroX2C.Blog.API.Modules.Posts;

public sealed class PostComment : EntityBase
{
    public const int MaxBodyLength = 4000;

    public required Guid PostId { get; set; }
    public Post Post { get; set; } = null!;
    public required Guid AuthorUserId { get; set; }
    public User AuthorUser { get; set; } = null!;
    public Guid? ParentCommentId { get; set; }
    public PostComment? ParentComment { get; set; }
    public List<PostComment> Replies { get; set; } = [];
    public List<PostCommentReplyState> ReplyStates { get; set; } = [];
    public required string Body { get; set; }
    public PostCommentStatus Status { get; set; } = PostCommentStatus.Visible;
}
