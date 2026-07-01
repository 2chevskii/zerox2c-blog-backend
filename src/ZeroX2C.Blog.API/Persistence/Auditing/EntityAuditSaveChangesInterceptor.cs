using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ZeroX2C.Blog.API.Modules.Shared;
using ZeroX2C.Blog.API.Modules.Users.Auth;

namespace ZeroX2C.Blog.API.Persistence.Auditing;

public sealed class EntityAuditSaveChangesInterceptor(
    IEnumerable<IAuditEntityChangeHandler> handlers,
    IAuthenticationContext authenticationContext,
    TimeProvider timeProvider
) : SaveChangesInterceptor
{
    private readonly IReadOnlyDictionary<EntityState, IAuditEntityChangeHandler> handlersByState =
        handlers.ToDictionary(handler => handler.State);

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result
    )
    {
        ApplyAudit(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        ApplyAudit(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAudit(DbContext? dbContext)
    {
        if (dbContext is null)
        {
            return;
        }

        dbContext.ChangeTracker.DetectChanges();

        var entries = dbContext
            .ChangeTracker.Entries<EntityBase>()
            .Where(entry => handlersByState.ContainsKey(entry.State))
            .ToArray();

        if (!authenticationContext.IsAuthenticated)
        {
            throw new InvalidOperationException(
                "Saving database entities requires authenticated user"
            );
        }

        foreach (var entry in entries)
        {
            handlersByState[entry.State].Handle(entry, GetAuditStamp());
        }
    }

    private AuditStamp GetAuditStamp() =>
        new AuditStamp(timeProvider.GetUtcNow().UtcDateTime, authenticationContext.UserId);
}
