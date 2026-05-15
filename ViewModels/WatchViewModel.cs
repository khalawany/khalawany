using KhalawanyTube.Models;

namespace KhalawanyTube.ViewModels;

public class WatchViewModel
{
    public MediaClip Clip { get; set; } = null!;
    public List<Comment> Comments { get; set; } = [];
    public int LikeCount { get; set; }
    public bool UserHasLiked { get; set; }
    public bool IsOwner { get; set; }
}
