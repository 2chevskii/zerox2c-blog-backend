using System.ComponentModel.DataAnnotations;

namespace ZeroX2C.Blog.API.Modules.Users.Contracts.Admin;

public sealed record BlockUserRequest([MaxLength(512)] string? Reason);
