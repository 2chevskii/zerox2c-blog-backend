using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ZeroX2C.Blog.API.Modules.Shared;

namespace ZeroX2C.Blog.API.Persistence.Auditing;

public sealed class DeletedEntityAuditHandler : IAuditEntityChangeHandler
{
    public EntityState State => EntityState.Deleted;

    public void Handle(EntityEntry<EntityBase> entry, AuditStamp stamp)
    {
        var entity = entry.Entity;
        var actorId = stamp.ActorId;

        entry.State = EntityState.Modified;
        entity.IsDeleted = true;
        entity.DeletedBy = actorId;
        entity.DeletedAt = stamp.UtcNow;
        entity.UpdatedBy = actorId;
        entity.UpdatedAt = stamp.UtcNow;

        entry.Property(nameof(EntityBase.CreatedBy)).IsModified = false;
        entry.Property(nameof(EntityBase.CreatedAt)).IsModified = false;
    }
}
