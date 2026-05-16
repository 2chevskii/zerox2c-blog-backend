using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ZeroX2C.Blog.API.Modules.Shared;

namespace ZeroX2C.Blog.API.Persistence.Auditing;

public interface IAuditEntityChangeHandler
{
    EntityState State { get; }

    void Handle(EntityEntry<EntityBase> entry, AuditStamp stamp);
}
