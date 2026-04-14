using KhalawanyTube.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KhalawanyTube.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly AppDbContext _db;

    public AdminController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.UsersCount = await _db.Users.CountAsync();
        ViewBag.ClipsCount = await _db.MediaClips.CountAsync();
        ViewBag.RecentClips = await _db.MediaClips
            .Include(x => x.Owner)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(20)
            .ToListAsync();

        return View();
    }
}
