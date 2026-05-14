using System.ComponentModel.DataAnnotations;

namespace ZeroX2C.Blog.API.Modules.Users.Admin.Contracts;

public sealed record BlockUserRequest([MaxLength(512)] string? Reason);
