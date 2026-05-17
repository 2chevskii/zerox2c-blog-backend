using System.ComponentModel.DataAnnotations;

namespace ZeroX2C.Blog.API.Modules.Users.Contracts.Profile;

public sealed record UploadAvatarRequest([Required] IFormFile File);
