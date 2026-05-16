using System.Text.RegularExpressions;

namespace ZeroX2C.Blog.API.Modules.Posts.Tags;

public static partial class TagName
{
    public const int MaxLength = 20;
    public const string Pattern = "^[a-z0-9]+(?:-[a-z0-9]+)*$";

    public static string Normalize(string name) => name.Trim();

    public static bool IsValid(string name) => NameRegex().IsMatch(name);

    [GeneratedRegex(Pattern)]
    private static partial Regex NameRegex();
}
