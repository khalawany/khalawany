using System.Security.Claims;
using KhalawanyTube.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KhalawanyTube.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin");

        var query = _db.MediaClips.Include(x => x.Owner).AsQueryable();
        if (!isAdmin)
        {
            query = query.Where(x => x.IsShared || (userId != null && x.OwnerId == userId));
        }

        var clips = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync();

        return View(clips);
    }
}
