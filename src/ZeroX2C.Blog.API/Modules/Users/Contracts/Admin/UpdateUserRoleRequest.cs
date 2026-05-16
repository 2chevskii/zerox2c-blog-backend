using System.ComponentModel.DataAnnotations;

namespace ZeroX2C.Blog.API.Modules.Users.Contracts.Admin;

public sealed record UpdateUserRoleRequest([Required] UserRole Role);
