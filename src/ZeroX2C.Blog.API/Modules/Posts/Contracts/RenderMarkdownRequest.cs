using System.ComponentModel.DataAnnotations;

namespace ZeroX2C.Blog.API.Modules.Posts.Contracts;

public sealed record RenderMarkdownRequest([Required] string Markdown, Guid? PostId);
