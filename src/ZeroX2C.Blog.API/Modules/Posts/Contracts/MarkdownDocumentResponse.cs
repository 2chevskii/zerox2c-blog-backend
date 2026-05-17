namespace ZeroX2C.Blog.API.Modules.Posts.Contracts;

public sealed record MarkdownDocumentResponse(
    string Markdown,
    string Html,
    string PlainText,
    int ReadingMinutes
);
