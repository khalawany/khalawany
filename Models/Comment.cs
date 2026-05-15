using System.ComponentModel.DataAnnotations;

namespace KhalawanyTube.Models;

public class Comment
{
    public int Id { get; set; }
    public int MediaClipId { get; set; }
    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsDeleteRequested { get; set; }
    public bool IsDeleted { get; set; }

    public MediaClip? MediaClip { get; set; }
    public ApplicationUser? User { get; set; }
}
