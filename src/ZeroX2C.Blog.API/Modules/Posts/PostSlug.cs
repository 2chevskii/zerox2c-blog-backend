using System.Text.RegularExpressions;

namespace ZeroX2C.Blog.API.Modules.Posts;

public static partial class PostSlug
{
    public const int MaxLength = 160;
    public const string Pattern = "^[a-z0-9]+(?:-[a-z0-9]+)*$";
    private const string FallbackSlug = "post";

    public static string? Normalize(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        return slug.Trim().ToLowerInvariant();
    }

    public static string CreateFromTitle(string title)
    {
        var normalized = NonSlugCharactersRegex()
            .Replace(title.Trim().ToLowerInvariant(), "-")
            .Trim('-');

        if (string.IsNullOrWhiteSpace(normalized))
        {
            normalized = FallbackSlug;
        }

        return TrimToMaxLength(normalized);
    }

    public static string AddNumericSuffix(string slug, int suffix)
    {
        var suffixText = $"-{suffix}";
        var maxBaseLength = MaxLength - suffixText.Length;
        var baseSlug = slug.Length > maxBaseLength ? slug[..maxBaseLength].Trim('-') : slug;

        return $"{baseSlug}{suffixText}";
    }

    public static bool IsValid(string slug) => SlugRegex().IsMatch(slug);

    private static string TrimToMaxLength(string slug) =>
        slug.Length > MaxLength ? slug[..MaxLength].Trim('-') : slug;

    [GeneratedRegex(Pattern)]
    private static partial Regex SlugRegex();

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonSlugCharactersRegex();
}
