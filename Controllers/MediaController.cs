using System.Security.Claims;
using KhalawanyTube.Data;
using KhalawanyTube.Models;
using KhalawanyTube.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KhalawanyTube.Controllers;

[Authorize]
public class MediaController : Controller
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public MediaController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<IActionResult> MyClips()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var clips = await _db.MediaClips.Where(x => x.OwnerId == userId).OrderByDescending(x => x.CreatedAtUtc).ToListAsync();
        return View(clips);
    }

    public IActionResult Upload() => View(new UploadClipViewModel());

    [HttpPost]
    public async Task<IActionResult> Upload(UploadClipViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var isValidContentType = model.MediaType == "audio"
            ? model.File.ContentType.StartsWith("audio/")
            : model.File.ContentType.StartsWith("video/");

        if (!isValidContentType)
        {
            ModelState.AddModelError(nameof(model.File), "File type does not match selected media type.");
            return View(model);
        }

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
        Directory.CreateDirectory(uploadsDir);

        var ext = Path.GetExtension(model.File.FileName);
        var fileName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(uploadsDir, fileName);

        await using (var stream = System.IO.File.Create(fullPath))
        {
            await model.File.CopyToAsync(stream);
        }

        var clip = new MediaClip
        {
            Title = model.Title,
            Description = model.Description,
            MediaType = model.MediaType,
            FilePath = $"/uploads/{fileName}",
            OwnerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.MediaClips.Add(clip);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(MyClips));
    }
}
