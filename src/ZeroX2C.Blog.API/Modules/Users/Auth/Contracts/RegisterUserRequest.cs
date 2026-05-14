using System.ComponentModel.DataAnnotations;

namespace ZeroX2C.Blog.API.Modules.Users.Auth.Contracts;

public sealed record RegisterUserRequest(
    [Required, MinLength(3), MaxLength(64)] string Username,
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MinLength(8), MaxLength(256)] string Password
);
