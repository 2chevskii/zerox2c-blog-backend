using System.ComponentModel.DataAnnotations;

namespace ZeroX2C.Blog.API.Modules.Users.Contracts.Auth;

public sealed record LoginRequest(
    [Required, MaxLength(256)] string Login,
    [Required, MaxLength(256)] string Password
);
