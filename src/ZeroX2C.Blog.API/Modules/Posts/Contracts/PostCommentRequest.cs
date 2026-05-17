using System.ComponentModel.DataAnnotations;

namespace ZeroX2C.Blog.API.Modules.Posts.Contracts;

public sealed record PostCommentRequest(
    [Required] [StringLength(PostComment.MaxBodyLength, MinimumLength = 1)] string Body,
    Guid? ParentCommentId = null
);
