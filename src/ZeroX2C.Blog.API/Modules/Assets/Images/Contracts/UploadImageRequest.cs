using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using ZeroX2C.Blog.API.Modules.Assets.Images;

namespace ZeroX2C.Blog.API.Modules.Assets.Images.Contracts;

public sealed class UploadImageRequest
{
    [Required]
    public IFormFile File { get; set; } = null!;

    [FromForm]
    public ImagePurpose Purpose { get; set; }
}
