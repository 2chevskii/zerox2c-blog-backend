using System.ComponentModel.DataAnnotations;
using ZeroX2C.Blog.API.Modules.Posts;

namespace ZeroX2C.Blog.API.Modules.Posts.Contracts;

public sealed record UpdatePostCommentRequest(
    [property: Required]
    [property: StringLength(PostComment.MaxBodyLength, MinimumLength = 1)]
    string Body
);
