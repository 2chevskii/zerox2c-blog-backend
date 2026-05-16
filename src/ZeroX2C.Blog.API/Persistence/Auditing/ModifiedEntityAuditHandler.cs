using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ZeroX2C.Blog.API.Modules.Shared;

namespace ZeroX2C.Blog.API.Persistence.Auditing;

public sealed class ModifiedEntityAuditHandler : IAuditEntityChangeHandler
{
    public EntityState State => EntityState.Modified;

    public void Handle(EntityEntry<EntityBase> entry, AuditStamp stamp)
    {
        var entity = entry.Entity;
        var actorId = stamp.ActorId;

        entity.UpdatedBy = actorId;
        entity.UpdatedAt = stamp.UtcNow;

        if (!entity.IsDeleted)
        {
            entity.DeletedBy = null;
            entity.DeletedAt = null;
        }

        entry.Property(nameof(EntityBase.CreatedBy)).IsModified = false;
        entry.Property(nameof(EntityBase.CreatedAt)).IsModified = false;
    }
}
