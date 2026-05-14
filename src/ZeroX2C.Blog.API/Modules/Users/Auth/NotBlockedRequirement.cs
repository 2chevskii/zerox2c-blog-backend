using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ZeroX2C.Blog.API.Persistence;

namespace ZeroX2C.Blog.API.Modules.Users.Auth;

public sealed class NotBlockedRequirement : IAuthorizationRequirement;

public sealed class NotBlockedRequirementHandler(BlogDbContext dbContext)
    : AuthorizationHandler<NotBlockedRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        NotBlockedRequirement requirement
    )
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            return;
        }

        var isBlocked = await dbContext.Users
            .Where(user => user.Id == parsedUserId)
            .Select(user => (bool?)user.IsBlocked)
            .SingleOrDefaultAsync();

        if (isBlocked == false)
        {
            context.Succeed(requirement);
        }
    }
}
