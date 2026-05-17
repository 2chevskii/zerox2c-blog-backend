using System.ComponentModel.DataAnnotations;

namespace ZeroX2C.Blog.API.Modules.Posts.Contracts;

public sealed record UploadPostMarkdownImageRequest([Required] IFormFile File);
