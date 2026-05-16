using ZeroX2C.Blog.API.Modules.Posts.Contracts.Tags;

namespace ZeroX2C.Blog.API.Modules.Posts.Tags;

public static class TagMapper
{
    public static TagResponse ToResponse(Tag tag) =>
        new(tag.Id, tag.Name, tag.Description);

    public static AdminTagResponse ToAdminResponse(Tag tag) =>
        new(
            tag.Id,
            tag.Name,
            tag.Description,
            tag.CreatedBy,
            tag.CreatedAt,
            tag.UpdatedBy,
            tag.UpdatedAt,
            tag.DeletedBy,
            tag.DeletedAt
        );
}
