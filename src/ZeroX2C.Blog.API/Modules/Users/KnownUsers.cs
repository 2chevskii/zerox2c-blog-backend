using ZeroX2C.Blog.API.Modules.Users.Auth;

namespace ZeroX2C.Blog.API.Modules.Users;

public static class KnownUsers
{
    public const string SystemUsername = "system";
    public const string SystemEmail = "system@internal.local";

    public const string SuperAdminUsername = "superadmin";
    public const string SuperAdminEmail = "superadmin@internal.local";
    public const string SuperAdminDefaultPassword = "superadmin";

    public static readonly Guid SystemId = new("00000000-0000-0000-0000-000000000001");
    public static readonly Guid SuperAdminId = new("00000000-0000-0000-0000-000000000002");

    public static readonly KnownUser System = new(
        SystemId,
        SystemUsername,
        SystemEmail,
        UserRole.SuperAdmin
    );

    public static readonly KnownUser SuperAdmin = new(
        SuperAdminId,
        SuperAdminUsername,
        SuperAdminEmail,
        UserRole.SuperAdmin
    );

    public sealed record KnownUser(Guid Id, string Username, string Email, UserRole Role)
    {
        public static implicit operator AuthenticationData(KnownUser knownUser) =>
            knownUser.ToAuthenticationData();

        public AuthenticationData ToAuthenticationData() =>
            new AuthenticationData.Authenticated(Id, Username, Email, Role);
    }

    public static bool IsKnownUserId(Guid userId) =>
        userId == System.Id || userId == SuperAdmin.Id;

    public static bool CanPasswordBeChanged(Guid userId) => userId == SuperAdmin.Id;
}
