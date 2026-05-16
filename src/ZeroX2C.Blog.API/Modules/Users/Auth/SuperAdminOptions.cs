namespace ZeroX2C.Blog.API.Modules.Users.Auth;

public sealed class SuperAdminOptions
{
    public const string SectionName = "SuperAdmin";

    public bool UseDefaultPassword { get; init; }
}
