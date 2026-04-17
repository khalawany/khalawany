using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace KhalawanyTube.Models;

public class ApplicationUser : IdentityUser
{
    [Required]
    [StringLength(60)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    public DateTime DateOfBirth { get; set; }

    [StringLength(300)]
    public string? Bio { get; set; }
}
