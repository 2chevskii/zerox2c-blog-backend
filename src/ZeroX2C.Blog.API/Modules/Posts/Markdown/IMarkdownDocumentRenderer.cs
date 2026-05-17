namespace ZeroX2C.Blog.API.Modules.Posts.Markdown;

public interface IMarkdownDocumentRenderer
{
    MarkdownDocumentRenderResult Render(
        string markdown,
        IReadOnlyCollection<PostMarkdownImage> availableImages
    );
}
