using System.Security.Claims;
using KhalawanyTube.Models;
using KhalawanyTube.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace KhalawanyTube.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> Edit()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        return View(new ProfileViewModel
        {
            DisplayName = user.DisplayName,
            DateOfBirth = user.DateOfBirth,
            Bio = user.Bio
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(ProfileViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var minAllowed = DateTime.UtcNow.AddYears(-7);
        if (model.DateOfBirth > minAllowed)
        {
            ModelState.AddModelError(nameof(model.DateOfBirth), "User must be at least 7 years old.");
            return View(model);
        }

        user.DisplayName = model.DisplayName;
        user.DateOfBirth = model.DateOfBirth;
        user.Bio = model.Bio;

        await _userManager.UpdateAsync(user);
        return RedirectToAction("Index", "Home");
    }
}
