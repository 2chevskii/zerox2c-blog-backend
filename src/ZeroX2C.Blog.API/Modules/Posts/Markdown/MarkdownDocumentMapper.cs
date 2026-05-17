using ZeroX2C.Blog.API.Modules.Posts.Contracts;

namespace ZeroX2C.Blog.API.Modules.Posts.Markdown;

public static class MarkdownDocumentMapper
{
    public static MarkdownDocumentResponse ToResponse(MarkdownDocumentContent document) =>
        new(
            document.Markdown,
            document.Html,
            document.PlainText,
            document.ReadingMinutes
        );

    public static PostMarkdownImageResponse ToResponse(PostMarkdownImage image) =>
        new(
            image.ImageId,
            image.Image.OriginalFileName,
            image.Image.ContentType,
            image.Image.SizeBytes,
            $"/api/images/{image.ImageId}",
            image.LocalPath,
            image.CreatedAt
        );
}
