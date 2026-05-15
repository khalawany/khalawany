namespace KhalawanyTube.Models;

public class Like
{
    public int Id { get; set; }
    public int MediaClipId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public MediaClip? MediaClip { get; set; }
    public ApplicationUser? User { get; set; }
}
