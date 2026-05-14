using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Routing.Constraints;

namespace ZeroX2C.Blog.API.CrossCutting.Api;

public partial class SlugRouteConstraint() : RegexRouteConstraint(SlugRegex())
{
    [GeneratedRegex("^[a-z0-9](?:[a-z0-9]+-[a-z0-9]+)*[a-z0-9]?$")]
    private static partial Regex SlugRegex();
}
