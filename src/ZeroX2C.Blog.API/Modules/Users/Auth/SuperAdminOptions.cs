using System.ComponentModel.DataAnnotations;

namespace ZeroX2C.Blog.API.Modules.Users.Auth;

public sealed class SuperAdminOptions
{
    public const string SectionName = "SuperAdmin";

    [Required, MinLength(3), MaxLength(64)]
    public string Username { get; init; } = string.Empty;

    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required, MinLength(8), MaxLength(256)]
    public string Password { get; init; } = string.Empty;
}
