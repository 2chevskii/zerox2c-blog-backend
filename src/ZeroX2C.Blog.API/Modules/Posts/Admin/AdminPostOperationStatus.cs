namespace ZeroX2C.Blog.API.Modules.Posts.Admin;

public enum AdminPostOperationStatus
{
    Success,
    PostNotFound,
    SlugAlreadyTaken,
    InvalidSlug,
    TagNotFound,
}
