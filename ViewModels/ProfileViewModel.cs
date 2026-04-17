using System.ComponentModel.DataAnnotations;

namespace KhalawanyTube.ViewModels;

public class ProfileViewModel
{
    [Required]
    [StringLength(60)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    public DateTime DateOfBirth { get; set; }

    [StringLength(300)]
    public string? Bio { get; set; }
}
