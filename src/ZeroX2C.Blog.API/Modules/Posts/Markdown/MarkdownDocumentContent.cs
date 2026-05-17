namespace ZeroX2C.Blog.API.Modules.Posts.Markdown;

public sealed class MarkdownDocumentContent
{
    public required string Markdown { get; set; }
    public required string Html { get; set; }
    public required string PlainText { get; set; }
    public int ReadingMinutes { get; set; }
}
