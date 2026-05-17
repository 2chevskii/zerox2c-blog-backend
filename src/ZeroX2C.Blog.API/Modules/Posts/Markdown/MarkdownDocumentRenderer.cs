using System.Text.RegularExpressions;
using Ganss.Xss;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace ZeroX2C.Blog.API.Modules.Posts.Markdown;

public sealed partial class MarkdownDocumentRenderer : IMarkdownDocumentRenderer
{
    private const int WordsPerMinute = 220;

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()
        .Build();

    public MarkdownDocumentRenderResult Render(
        string markdown,
        IReadOnlyCollection<PostMarkdownImage> availableImages
    )
    {
        var normalizedMarkdown = markdown.Trim();
        var document = Markdig.Markdown.Parse(normalizedMarkdown, Pipeline);
        var imageUrlMap = availableImages
            .Where(image => !image.IsDeleted && !image.Image.IsDeleted)
            .ToDictionary(image => image.LocalPath, image => $"/api/images/{image.ImageId}");
        var invalidImagePaths = RewriteLocalImageUrls(document, imageUrlMap);

        if (invalidImagePaths.Count > 0)
        {
            return MarkdownDocumentRenderResult.Failure(invalidImagePaths);
        }

        var html = document.ToHtml(Pipeline);
        var sanitizedHtml = CreateSanitizer().Sanitize(html);
        var plainText = Markdig.Markdown.ToPlainText(normalizedMarkdown, Pipeline).Trim();
        var readingMinutes = EstimateReadingMinutes(plainText);

        return MarkdownDocumentRenderResult.Success(
            new MarkdownDocumentContent
            {
                Markdown = normalizedMarkdown,
                Html = sanitizedHtml,
                PlainText = plainText,
                ReadingMinutes = readingMinutes,
            }
        );
    }

    private static IReadOnlyCollection<string> RewriteLocalImageUrls(
        Markdig.Syntax.MarkdownDocument document,
        IReadOnlyDictionary<string, string> imageUrlMap
    )
    {
        var invalidImagePaths = new List<string>();

        foreach (var image in document.Descendants<LinkInline>().Where(link => link.IsImage))
        {
            var url = image.Url;
            if (!TryNormalizeLocalImagePath(url, out var localPath))
            {
                continue;
            }

            if (imageUrlMap.TryGetValue(localPath, out var resolvedUrl))
            {
                image.Url = resolvedUrl;
                continue;
            }

            invalidImagePaths.Add(url ?? string.Empty);
        }

        return invalidImagePaths;
    }

    private static bool TryNormalizeLocalImagePath(string? url, out string localPath)
    {
        localPath = string.Empty;
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var normalizedUrl = url.Trim().Replace('\\', '/');
        if (Uri.TryCreate(normalizedUrl, UriKind.Absolute, out _))
        {
            return false;
        }

        if (normalizedUrl.StartsWith("./", StringComparison.Ordinal))
        {
            normalizedUrl = normalizedUrl[2..];
        }

        var match = LocalImagePathRegex().Match(normalizedUrl);
        if (!match.Success)
        {
            return normalizedUrl.StartsWith("images/", StringComparison.OrdinalIgnoreCase);
        }

        localPath = $"images/{match.Groups["id"].Value}";
        return true;
    }

    private static HtmlSanitizer CreateSanitizer()
    {
        var sanitizer = new HtmlSanitizer();

        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.Add("http");
        sanitizer.AllowedSchemes.Add("https");
        sanitizer.AllowedSchemes.Add("mailto");

        sanitizer.AllowedAttributes.Add("class");
        sanitizer.AllowedAttributes.Add("target");
        sanitizer.AllowedAttributes.Add("rel");

        return sanitizer;
    }

    private static int EstimateReadingMinutes(string plainText)
    {
        var wordCount = WordRegex().Matches(plainText).Count;
        return Math.Max(1, (int)Math.Ceiling(wordCount / (double)WordsPerMinute));
    }

    [GeneratedRegex(
        "^images/(?<id>[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})$"
    )]
    private static partial Regex LocalImagePathRegex();

    [GeneratedRegex(@"\S+")]
    private static partial Regex WordRegex();
}
