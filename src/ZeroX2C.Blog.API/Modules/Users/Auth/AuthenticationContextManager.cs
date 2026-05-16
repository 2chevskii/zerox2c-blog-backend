using System.Threading;
using ZeroX2C.Blog.API.Modules.Users;

namespace ZeroX2C.Blog.API.Modules.Users.Auth;

public sealed class AuthenticationContextManager : IAuthenticationContextManager
{
    private readonly AsyncLocal<AuthenticationData?> currentData = new();
    private readonly IAuthenticationContext current;

    public AuthenticationContextManager()
    {
        current = new CurrentAuthenticationContext(this);
    }

    public IAuthenticationContext Current => current;

    public IAuthenticationScope As(AuthenticationData authenticationData)
    {
        ArgumentNullException.ThrowIfNull(authenticationData);

        var previousData = currentData.Value;
        currentData.Value = authenticationData;

        return new AuthenticationScope(this, previousData);
    }

    private AuthenticationData CurrentData =>
        currentData.Value ?? new AuthenticationData.Anonymous();

    private sealed class CurrentAuthenticationContext(AuthenticationContextManager manager)
        : IAuthenticationContext
    {
        public AuthenticationData Data => manager.CurrentData;

        public bool IsAuthenticated => manager.CurrentData is AuthenticationData.Authenticated;

        public Guid UserId => AuthenticatedData.UserId;

        public Guid? MaybeUserId => MaybeAuthenticatedData?.UserId;

        public string Username => AuthenticatedData.Username;

        public string? MaybeUsername => MaybeAuthenticatedData?.Username;

        public string? Email => AuthenticatedData.Email;

        public string? MaybeEmail => MaybeAuthenticatedData?.Email;

        public UserRole Role => AuthenticatedData.Role;

        public UserRole? MaybeRole => MaybeAuthenticatedData?.Role;

        public bool IsBlocked => AuthenticatedData.IsBlocked;

        public bool? MaybeIsBlocked => MaybeAuthenticatedData?.IsBlocked;

        private AuthenticationData.Authenticated AuthenticatedData =>
            MaybeAuthenticatedData
            ?? throw new InvalidOperationException("Current authentication context is anonymous.");

        private AuthenticationData.Authenticated? MaybeAuthenticatedData =>
            manager.CurrentData as AuthenticationData.Authenticated;
    }

    private sealed class AuthenticationScope(
        AuthenticationContextManager manager,
        AuthenticationData? previousData
    ) : IAuthenticationScope
    {
        private bool disposed;

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            manager.currentData.Value = previousData;
            disposed = true;
        }
    }
}
