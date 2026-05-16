namespace ZeroX2C.Blog.API.Modules.Users.Admin;

public enum AdminUserOperationStatus
{
    Success,
    UserNotFound,
    SuperAdminAlreadyExists,
    CannotBlockSuperAdmin,
    KnownUserCannotBeModified,
    PasswordCannotBeChanged,
}
