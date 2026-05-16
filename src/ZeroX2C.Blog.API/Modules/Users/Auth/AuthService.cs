using Microsoft.EntityFrameworkCore;
using ZeroX2C.Blog.API.Modules.Users.Contracts.Auth;
using ZeroX2C.Blog.API.Persistence;

namespace ZeroX2C.Blog.API.Modules.Users.Auth;

public sealed class AuthService(
    BlogDbContext dbContext,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    ISteamOpenIdClient steamOpenIdClient
) : IAuthService
{
    public string CreateSteamAuthenticationUrl(string returnTo, string realm) =>
        steamOpenIdClient.CreateAuthenticationUrl(returnTo, realm);

    public async Task<AuthOperationResult<AuthResponse>> RegisterAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken
    )
    {
        var username = UserNormalization.NormalizeUsername(request.Username);
        var email = UserNormalization.NormalizeEmail(request.Email);

        var usernameExists = await dbContext.Users.AnyAsync(
            user => user.Username == username,
            cancellationToken
        );
        if (usernameExists)
        {
            return AuthOperationResult<AuthResponse>.Failure(
                AuthOperationStatus.UsernameAlreadyTaken
            );
        }

        var emailExists = await dbContext.Users.AnyAsync(
            user => user.Email == email,
            cancellationToken
        );
        if (emailExists)
        {
            return AuthOperationResult<AuthResponse>.Failure(
                AuthOperationStatus.EmailAlreadyTaken
            );
        }

        var userId = Guid.CreateVersion7();
        var user = new User
        {
            Id = userId,
            Username = username,
            Email = email,
            PasswordHash = passwordHasher.HashPassword(request.Password),
            Role = UserRole.User,
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return AuthOperationResult<AuthResponse>.Success(
            await CreateAuthResponseAsync(user, cancellationToken)
        );
    }

    public async Task<AuthOperationResult<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken
    )
    {
        var normalizedUsername = UserNormalization.NormalizeUsername(request.Login);
        var normalizedEmail = UserNormalization.NormalizeEmail(request.Login);
        var user =
            await dbContext.Users.SingleOrDefaultAsync(
                existingUser => existingUser.Username == normalizedUsername,
                cancellationToken
            )
            ?? await dbContext.Users.SingleOrDefaultAsync(
                existingUser => existingUser.Email == normalizedEmail,
                cancellationToken
            );

        if (user is null || !passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return AuthOperationResult<AuthResponse>.Failure(
                AuthOperationStatus.InvalidCredentials
            );
        }

        return AuthOperationResult<AuthResponse>.Success(
            await CreateAuthResponseAsync(user, cancellationToken)
        );
    }

    public async Task<AuthOperationResult<AuthResponse>> LoginWithSteamAsync(
        SteamOpenIdCallback callback,
        string expectedReturnTo,
        CancellationToken cancellationToken
    )
    {
        var validationResult = await steamOpenIdClient.ValidateCallbackAsync(
            callback,
            expectedReturnTo,
            cancellationToken
        );
        if (!validationResult.IsValid || validationResult.SteamId is null)
        {
            return AuthOperationResult<AuthResponse>.Failure(
                AuthOperationStatus.ExternalLoginFailed
            );
        }

        var externalLogin = await dbContext
            .UserExternalLogins.Include(login => login.User)
            .SingleOrDefaultAsync(
                login =>
                    login.Provider == ExternalAuthProviderNames.Steam
                    && login.ProviderUserId == validationResult.SteamId,
                cancellationToken
            );

        if (externalLogin is not null)
        {
            if (externalLogin.User.IsBlocked)
            {
                return AuthOperationResult<AuthResponse>.Failure(AuthOperationStatus.UserBlocked);
            }

            return AuthOperationResult<AuthResponse>.Success(
                await CreateAuthResponseAsync(externalLogin.User, cancellationToken)
            );
        }

        var userId = Guid.CreateVersion7();
        var user = new User
        {
            Id = userId,
            Username = UserNormalization.NormalizeUsername($"steam_{validationResult.SteamId}"),
            Email = UserNormalization.NormalizeEmail(
                $"steam-{validationResult.SteamId}@external.local"
            ),
            PasswordHash = [],
            EmailConfirmed = true,
            Role = UserRole.User,
            ExternalLogins =
            [
                new UserExternalLogin
                {
                    Id = Guid.CreateVersion7(),
                    UserId = userId,
                    Provider = ExternalAuthProviderNames.Steam,
                    ProviderUserId = validationResult.SteamId,
                    ProviderDisplayName = validationResult.SteamId,
                },
            ],
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return AuthOperationResult<AuthResponse>.Success(
            await CreateAuthResponseAsync(user, cancellationToken)
        );
    }

    public async Task<AuthOperationResult<CurrentUserResponse>> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(
            existingUser => existingUser.Id == userId,
            cancellationToken
        );
        if (user is null)
        {
            return AuthOperationResult<CurrentUserResponse>.Failure(
                AuthOperationStatus.UserNotFound
            );
        }

        return AuthOperationResult<CurrentUserResponse>.Success(
            new CurrentUserResponse(
                user.Id,
                user.Username,
                user.Email,
                user.IsBlocked,
                user.Role
            )
        );
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(
        User user,
        CancellationToken cancellationToken
    )
    {
        var token = await jwtTokenService.CreateAccessTokenAsync(user, cancellationToken);

        return new AuthResponse(
            user.Id,
            user.Username,
            user.Email,
            token.Value,
            token.ExpiresAt,
            user.Role
        );
    }
}
