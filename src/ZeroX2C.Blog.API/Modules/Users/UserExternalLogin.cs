namespace ZeroX2C.Blog.API.Modules.Users;

public sealed class UserExternalLogin
{
    public required Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public required string Provider { get; set; }
    public required string ProviderUserId { get; set; }
    public string? ProviderDisplayName { get; set; }
}
