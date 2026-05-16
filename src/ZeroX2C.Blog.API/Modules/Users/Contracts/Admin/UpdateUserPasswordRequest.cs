using System.ComponentModel.DataAnnotations;

namespace ZeroX2C.Blog.API.Modules.Users.Contracts.Admin;

public sealed record UpdateUserPasswordRequest(
    [Required, MinLength(8), MaxLength(256)] string Password
);
