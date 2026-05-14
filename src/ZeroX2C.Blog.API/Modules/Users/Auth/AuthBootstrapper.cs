using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ZeroX2C.Blog.API.Persistence;

namespace ZeroX2C.Blog.API.Modules.Users.Auth;

public sealed class AuthBootstrapper(
    BlogDbContext dbContext,
    IPasswordHasher passwordHasher,
    IOptions<SuperAdminOptions> superAdminOptions
)
{
    public async Task BootstrapAsync(CancellationToken cancellationToken = default)
    {
        await EnsureExactlyOneSuperAdminAsync(cancellationToken);
    }

    private async Task EnsureExactlyOneSuperAdminAsync(CancellationToken cancellationToken)
    {
        var superAdminCount = await dbContext.Users.CountAsync(
            user => user.Role == UserRole.SuperAdmin,
            cancellationToken
        );

        if (superAdminCount == 1)
        {
            return;
        }

        if (superAdminCount > 1)
        {
            throw new InvalidOperationException(
                "Invalid auth state: more than one SuperAdmin user exists."
            );
        }

        var options = superAdminOptions.Value;
        ValidateSuperAdminOptions(options);

        var email = UserNormalization.NormalizeEmail(options.Email);
        var user = await dbContext.Users.SingleOrDefaultAsync(
            existingUser => existingUser.Email == email,
            cancellationToken
        );

        if (user is null)
        {
            var userId = Guid.NewGuid();
            user = new User
            {
                Id = userId,
                CreatedBy = userId,
                Username = UserNormalization.NormalizeUsername(options.Username),
                Email = email,
                EmailConfirmed = true,
                PasswordHash = passwordHasher.HashPassword(options.Password),
                Role = UserRole.SuperAdmin,
                CreatedAt = DateTime.UtcNow,
            };

            dbContext.Users.Add(user);
        }
        else
        {
            user.Role = UserRole.SuperAdmin;
            user.EmailConfirmed = true;
            user.UpdatedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void ValidateSuperAdminOptions(SuperAdminOptions options)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(options);

        if (Validator.TryValidateObject(options, validationContext, validationResults, true))
        {
            return;
        }

        var errors = string.Join("; ", validationResults.Select(result => result.ErrorMessage));
        throw new InvalidOperationException(
            $"SuperAdmin bootstrap settings are invalid: {errors}"
        );
    }
}
