using System.ComponentModel.DataAnnotations;

namespace ZeroX2C.Blog.API.Modules.Users.Admin.Contracts;

public sealed record UpdateUserRoleRequest([Required] UserRole Role);
