using KhalawanyTube.Models;

namespace KhalawanyTube.ViewModels;

public class AdminUserDetailViewModel
{
    public string Id          { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email       { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string? Bio        { get; set; }
    public bool IsBlocked     { get; set; }
    public bool IsAdmin       { get; set; }
    public List<MediaClip> Clips { get; set; } = new();
}
