using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ZeroX2C.Blog.API.Modules.Users.Auth;

public abstract record AuthenticationData
{
    public sealed record Anonymous : AuthenticationData;

    public sealed record Authenticated(
        Guid UserId,
        string Username,
        string? Email,
        UserRole Role,
        bool IsBlocked = false
    ) : AuthenticationData;

    public static AuthenticationData FromPrincipal(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return new Anonymous();
        }

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            return new Anonymous();
        }

        var username = principal.FindFirstValue(ClaimTypes.Name)
            ?? principal.Identity.Name;
        if (string.IsNullOrWhiteSpace(username))
        {
            return new Anonymous();
        }

        var role = principal.FindFirstValue(ClaimTypes.Role);
        if (!Enum.TryParse<UserRole>(role, ignoreCase: true, out var parsedRole))
        {
            return new Anonymous();
        }

        var email = principal.FindFirstValue(ClaimTypes.Email)
            ?? principal.FindFirstValue(JwtRegisteredClaimNames.Email);
        var isBlocked = bool.TryParse(
            principal.FindFirstValue("is_blocked"),
            out var parsedIsBlocked
        ) && parsedIsBlocked;

        return new Authenticated(
            parsedUserId,
            username,
            email,
            parsedRole,
            isBlocked
        );
    }
}
