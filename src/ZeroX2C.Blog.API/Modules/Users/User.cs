using ZeroX2C.Blog.API.Modules.Shared;
using ZeroX2C.Blog.API.Modules.Posts;

namespace ZeroX2C.Blog.API.Modules.Users;

public sealed class User : EntityBase
{
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required byte[] PasswordHash { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public bool EmailConfirmed { get; set; }
    public bool IsBlocked { get; set; }
    public DateTimeOffset? BlockedAt { get; set; }
    public string? BlockedReason { get; set; }

    public List<UserExternalLogin> ExternalLogins { get; set; } = [];
    public List<PostReaction> PostReactions { get; set; } = [];
}
