using KhalawanyTube.Data;
using KhalawanyTube.Models;
using KhalawanyTube.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KhalawanyTube.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;

    public AdminController(AppDbContext db, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
    {
        _db = db;
        _userManager = userManager;
        _env = env;
    }

    // ── Dashboard ──────────────────────────────────────────────────────────

    public async Task<IActionResult> Index()
    {
        ViewBag.UsersCount  = await _db.Users.CountAsync();
        ViewBag.ClipsCount  = await _db.MediaClips.CountAsync();
        ViewBag.RecentClips = await _db.MediaClips
            .Include(x => x.Owner)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(20)
            .ToListAsync();

        return View();
    }

    // ── Users list ─────────────────────────────────────────────────────────

    public async Task<IActionResult> Users()
    {
        var users = await _db.Users
            .OrderBy(u => u.DisplayName)
            .ToListAsync();

        var clipCounts = await _db.MediaClips
            .GroupBy(c => c.OwnerId)
            .Select(g => new { OwnerId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.OwnerId, x => x.Count);

        ViewBag.ClipCounts = clipCounts;

        var adminIds = (await _userManager.GetUsersInRoleAsync("Admin"))
            .Select(u => u.Id)
            .ToHashSet();
        ViewBag.AdminIds = adminIds;

        return View(users);
    }

    // ── User detail ────────────────────────────────────────────────────────

    public async Task<IActionResult> UserDetail(string id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound();

        var clips = await _db.MediaClips
            .Where(c => c.OwnerId == id)
            .OrderByDescending(c => c.CreatedAtUtc)
            .ToListAsync();

        var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

        var vm = new AdminUserDetailViewModel
        {
            Id          = user.Id,
            DisplayName = user.DisplayName,
            Email       = user.Email ?? string.Empty,
            DateOfBirth = user.DateOfBirth,
            Bio         = user.Bio,
            IsBlocked   = user.IsBlocked,
            IsAdmin     = isAdmin,
            Clips       = clips
        };
        return View(vm);
    }

    // ── Block / unblock user ───────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> BlockUser(string id, string returnTo = "Users")
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound();

        // Cannot block another admin
        if (await _userManager.IsInRoleAsync(user, "Admin"))
            return BadRequest("Cannot block an admin account.");

        user.IsBlocked = !user.IsBlocked;
        await _db.SaveChangesAsync();

        return returnTo == "Detail"
            ? RedirectToAction(nameof(UserDetail), new { id })
            : RedirectToAction(nameof(Users));
    }

    // ── Delete user ────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound();

        if (await _userManager.IsInRoleAsync(user, "Admin"))
            return BadRequest("Cannot delete an admin account.");

        // Delete all their clip files + DB rows
        var clips = await _db.MediaClips.Where(c => c.OwnerId == id).ToListAsync();
        foreach (var clip in clips)
        {
            var filePath = Path.Combine(
                _env.WebRootPath,
                clip.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
        }
        _db.MediaClips.RemoveRange(clips);

        await _userManager.DeleteAsync(user);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Users));
    }

    // ── Block / unblock clip ───────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> BlockClip(int id, string returnTo = "Index")
    {
        var clip = await _db.MediaClips.FindAsync(id);
        if (clip is null) return NotFound();

        clip.IsBlocked = !clip.IsBlocked;
        await _db.SaveChangesAsync();

        if (returnTo == "Detail")
            return RedirectToAction(nameof(UserDetail), new { id = clip.OwnerId });

        return RedirectToAction(nameof(Index));
    }

    // ── Delete clip (admin) ────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> DeleteClip(int id, string returnTo = "Index")
    {
        var clip = await _db.MediaClips.FindAsync(id);
        if (clip is null) return NotFound();

        var ownerId  = clip.OwnerId;
        var filePath = Path.Combine(
            _env.WebRootPath,
            clip.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);

        _db.MediaClips.Remove(clip);
        await _db.SaveChangesAsync();

        if (returnTo == "Detail")
            return RedirectToAction(nameof(UserDetail), new { id = ownerId });

        return RedirectToAction(nameof(Index));
    }
}
