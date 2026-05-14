namespace ZeroX2C.Blog.API.Modules.Shared;

public abstract class EntityBase
{
    public required Guid Id { get; set; }

    public required Guid CreatedBy { get; set; }
    public required DateTime CreatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public Guid? DeletedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
}
