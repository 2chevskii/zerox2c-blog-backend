using Microsoft.EntityFrameworkCore;
using ZeroX2C.Blog.API.Modules.Users.Auth.Contracts;
using ZeroX2C.Blog.API.Persistence;

namespace ZeroX2C.Blog.API.Modules.Users.Auth;

public interface IAuthService
{
    Task<AuthOperationResult<AuthResponse>> RegisterAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken
    );

    Task<AuthOperationResult<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken
    );

    Task<AuthOperationResult<CurrentUserResponse>> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken
    );
}

public enum AuthOperationStatus
{
    Success,
    UsernameAlreadyTaken,
    EmailAlreadyTaken,
    InvalidCredentials,
    UserNotFound,
}

public sealed record AuthOperationResult<T>(AuthOperationStatus Status, T? Value = default)
{
    public static AuthOperationResult<T> Success(T value) =>
        new(AuthOperationStatus.Success, value);

    public static AuthOperationResult<T> Failure(AuthOperationStatus status) =>
        new(status);
}

public sealed class AuthService(
    BlogDbContext dbContext,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService
) : IAuthService
{
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

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            CreatedBy = userId,
            Username = username,
            Email = email,
            PasswordHash = passwordHasher.HashPassword(request.Password),
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
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
        var normalizedLogin = request.Login.Trim().ToUpperInvariant();
        var user =
            await dbContext.Users.SingleOrDefaultAsync(
                existingUser => existingUser.Username == normalizedLogin,
                cancellationToken
            )
            ?? await dbContext.Users.SingleOrDefaultAsync(
                existingUser => existingUser.Email == normalizedLogin,
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
