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
        var clips = await _db.MediaClips
            .Include(x => x.Owner)
            .Where(x => x.IsShared && !x.IsBlocked && !(x.Owner != null && x.Owner.IsBlocked))
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync();
        return View(clips);
    }
}
