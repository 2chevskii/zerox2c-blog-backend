using System.Text.RegularExpressions;

namespace ZeroX2C.Blog.API.Modules.Posts;

internal static partial class PostKeywordExtractor
{
    private const double TitleWeight = 4;
    private const double SubtitleWeight = 2;
    private const double BodyWeight = 1;

    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "about",
        "after",
        "again",
        "against",
        "also",
        "because",
        "before",
        "being",
        "between",
        "could",
        "during",
        "each",
        "from",
        "have",
        "into",
        "more",
        "most",
        "only",
        "other",
        "over",
        "same",
        "should",
        "some",
        "such",
        "than",
        "that",
        "their",
        "then",
        "there",
        "these",
        "they",
        "this",
        "through",
        "under",
        "using",
        "very",
        "were",
        "what",
        "when",
        "where",
        "which",
        "while",
        "with",
        "without",
        "would",
        "your",
    };

    private static readonly HashSet<string> AllowedShortKeywords = new(StringComparer.Ordinal)
    {
        "ai",
        "go",
        "js",
        "ts",
        "ui",
        "ux",
    };

    public static IReadOnlyCollection<string> ExtractTopKeywords(
        IReadOnlyCollection<PostKeywordSource> sources,
        string? search,
        int limit
    )
    {
        var normalizedSearch = NormalizeSearchTerm(search);

        var documentFrequencies = new Dictionary<string, int>(StringComparer.Ordinal);
        var documentTermFrequencies = new List<Dictionary<string, double>>(sources.Count);

        foreach (var source in sources)
        {
            var termFrequencies = new Dictionary<string, double>(StringComparer.Ordinal);
            AddWeightedTermFrequencies(termFrequencies, source.Title, TitleWeight);
            AddWeightedTermFrequencies(termFrequencies, source.Subtitle, SubtitleWeight);
            AddWeightedTermFrequencies(termFrequencies, source.Body, BodyWeight);

            if (termFrequencies.Count == 0)
            {
                continue;
            }

            documentTermFrequencies.Add(termFrequencies);

            foreach (var term in termFrequencies.Keys)
            {
                documentFrequencies[term] = documentFrequencies.GetValueOrDefault(term) + 1;
            }
        }

        var documentCount = documentTermFrequencies.Count;
        if (documentCount == 0)
        {
            return [];
        }

        var scores = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var termFrequencies in documentTermFrequencies)
        {
            foreach (var (term, weightedFrequency) in termFrequencies)
            {
                var inverseDocumentFrequency =
                    Math.Log((documentCount + 1.0) / (documentFrequencies[term] + 1.0)) + 1.0;
                scores[term] = scores.GetValueOrDefault(term)
                    + (1.0 + Math.Log(weightedFrequency)) * inverseDocumentFrequency;
            }
        }

        return scores
            .Where(score =>
                normalizedSearch is null
                || score.Key.Contains(normalizedSearch, StringComparison.Ordinal)
            )
            .OrderByDescending(score =>
                normalizedSearch is not null
                && score.Key.StartsWith(normalizedSearch, StringComparison.Ordinal)
            )
            .ThenByDescending(score => score.Value)
            .ThenBy(score => score.Key, StringComparer.Ordinal)
            .Take(limit)
            .Select(score => score.Key)
            .ToArray();
    }

    public static string? NormalizeSearchTerm(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var match = KeywordRegex().Match(value.Trim().ToLowerInvariant());
        return match.Success ? NormalizeKeyword(match.Value) : null;
    }

    private static void AddWeightedTermFrequencies(
        Dictionary<string, double> termFrequencies,
        string? text,
        double weight
    )
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        foreach (Match match in KeywordRegex().Matches(text.ToLowerInvariant()))
        {
            var keyword = NormalizeKeyword(match.Value);
            if (keyword is null)
            {
                continue;
            }

            termFrequencies[keyword] = termFrequencies.GetValueOrDefault(keyword) + weight;
        }
    }

    private static string? NormalizeKeyword(string rawKeyword)
    {
        var keyword = rawKeyword switch
        {
            "asp.net" => "aspnet",
            "c#" => "csharp",
            "c++" => "cpp",
            "f#" => "fsharp",
            ".net" => "dotnet",
            _ => rawKeyword.Trim('.').Replace(".", string.Empty).Replace("#", "sharp"),
        };

        if (
            (keyword.Length < 3 && !AllowedShortKeywords.Contains(keyword))
            || StopWords.Contains(keyword)
            || keyword.All(char.IsDigit)
        )
        {
            return null;
        }

        return keyword;
    }

    [GeneratedRegex(@"(?i)(?:c\+\+|[cf]#|\.net|[\p{L}\p{N}]+(?:[.#][\p{L}\p{N}]+)*)")]
    private static partial Regex KeywordRegex();
}

internal sealed record PostKeywordSource(string Title, string? Subtitle, string Body);
