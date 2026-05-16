using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ZeroX2C.Blog.API.Modules.Shared;

namespace ZeroX2C.Blog.API.Persistence.Auditing;

public sealed class AddedEntityAuditHandler : IAuditEntityChangeHandler
{
    public EntityState State => EntityState.Added;

    public void Handle(EntityEntry<EntityBase> entry, AuditStamp stamp)
    {
        var entity = entry.Entity;
        var actorId = stamp.ActorId;

        entity.CreatedBy = actorId;
        entity.CreatedAt = stamp.UtcNow;
        entity.UpdatedBy = null;
        entity.UpdatedAt = null;
        entity.IsDeleted = false;
        entity.DeletedBy = null;
        entity.DeletedAt = null;
    }
}
