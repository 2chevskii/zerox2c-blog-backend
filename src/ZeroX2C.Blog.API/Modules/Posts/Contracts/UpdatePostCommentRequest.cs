using System.ComponentModel.DataAnnotations;
using ZeroX2C.Blog.API.Modules.Posts;

namespace ZeroX2C.Blog.API.Modules.Posts.Contracts;

public sealed record UpdatePostCommentRequest(
    [Required] [StringLength(PostComment.MaxBodyLength, MinimumLength = 1)] string Body
);
