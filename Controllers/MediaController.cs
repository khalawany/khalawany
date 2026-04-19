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
        var clips = await _db.MediaClips
            .Where(x => x.OwnerId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync();

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
        if (string.IsNullOrWhiteSpace(ext))
        {
            ext = model.MediaType == "audio" ? ".webm" : ".webm";
        }

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
            IsShared = model.IsShared,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.MediaClips.Add(clip);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(MyClips));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var clip = await _db.MediaClips.FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == userId);
        if (clip is null) return NotFound();

        var model = new EditClipViewModel
        {
            Id = clip.Id,
            Title = clip.Title,
            Description = clip.Description,
            MediaType = clip.MediaType,
            IsShared = clip.IsShared
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(EditClipViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var clip = await _db.MediaClips.FirstOrDefaultAsync(x => x.Id == model.Id && x.OwnerId == userId);
        if (clip is null) return NotFound();

        clip.Title = model.Title;
        clip.Description = model.Description;
        clip.MediaType = model.MediaType;
        clip.IsShared = model.IsShared;

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(MyClips));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var clip = await _db.MediaClips.FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == userId);
        if (clip is null) return NotFound();

        var fullPath = Path.Combine(_env.WebRootPath, clip.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(fullPath))
        {
            System.IO.File.Delete(fullPath);
        }

        _db.MediaClips.Remove(clip);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(MyClips));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleShare(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var clip = await _db.MediaClips.FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == userId);
        if (clip is null) return NotFound();

        clip.IsShared = !clip.IsShared;
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(MyClips));
    }
}
