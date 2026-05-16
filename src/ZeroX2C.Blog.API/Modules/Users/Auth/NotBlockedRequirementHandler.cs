using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ZeroX2C.Blog.API.Persistence;

namespace ZeroX2C.Blog.API.Modules.Users.Auth;

public sealed class NotBlockedRequirementHandler(
    BlogDbContext dbContext,
    IAuthenticationContext authenticationContext
)
    : AuthorizationHandler<NotBlockedRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        NotBlockedRequirement requirement
    )
    {
        if (authenticationContext.MaybeUserId is not { } userId)
        {
            return;
        }

        var isBlocked = await dbContext.Users
            .Where(user => user.Id == userId)
            .Select(user => (bool?)user.IsBlocked)
            .SingleOrDefaultAsync();

        if (isBlocked == false)
        {
            context.Succeed(requirement);
        }
    }
}
