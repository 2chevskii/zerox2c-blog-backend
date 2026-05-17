namespace ZeroX2C.Blog.API.Modules.Posts.Markdown;

public sealed record MarkdownDocumentRenderResult(
    MarkdownDocumentContent? Document,
    IReadOnlyCollection<string> InvalidImagePaths
)
{
    public bool IsSuccess => InvalidImagePaths.Count == 0 && Document is not null;

    public static MarkdownDocumentRenderResult Success(MarkdownDocumentContent document) =>
        new(document, []);

    public static MarkdownDocumentRenderResult Failure(IReadOnlyCollection<string> invalidImagePaths) =>
        new(null, invalidImagePaths);
}
