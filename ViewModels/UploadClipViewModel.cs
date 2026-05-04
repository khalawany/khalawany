using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace KhalawanyTube.ViewModels;

public class UploadClipViewModel
{
    [Required]
    [StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [RegularExpression("video|audio")]
    public string MediaType { get; set; } = "video";

    [Required]
    public IFormFile File { get; set; } = default!;

    public bool IsShared { get; set; }
}
