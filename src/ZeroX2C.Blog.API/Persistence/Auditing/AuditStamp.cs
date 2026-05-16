namespace ZeroX2C.Blog.API.Persistence.Auditing;

public sealed record AuditStamp(DateTime UtcNow, Guid ActorId);
