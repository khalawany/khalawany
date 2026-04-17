using System.ComponentModel.DataAnnotations;

namespace KhalawanyTube.ViewModels;

public class EditClipViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [RegularExpression("video|audio")]
    public string MediaType { get; set; } = "video";

    public bool IsShared { get; set; }
}
