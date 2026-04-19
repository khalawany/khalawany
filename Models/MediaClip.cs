using System.ComponentModel.DataAnnotations;

namespace KhalawanyTube.Models;

public class MediaClip
{
    public int Id { get; set; }

    [Required]
    [StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [StringLength(20)]
    public string MediaType { get; set; } = "video"; // video or audio

    [Required]
    public string FilePath { get; set; } = string.Empty;

    [Required]
    public string OwnerId { get; set; } = string.Empty;

    public bool IsShared { get; set; } = false;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ApplicationUser? Owner { get; set; }
}
