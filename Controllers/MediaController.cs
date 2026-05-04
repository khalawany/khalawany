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
    private const long MaxFileSize = 200 * 1024 * 1024; // 200 MB

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
        return View(clips);
    }

    public IActionResult Upload() => View(new UploadClipViewModel());

    [HttpPost]
    [RequestSizeLimit(209715200)]
    public async Task<IActionResult> Upload(UploadClipViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        if (model.File.Length > MaxFileSize)
        {
            ModelState.AddModelError(nameof(model.File), "File must be under 200 MB.");
            return View(model);
        }

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
            CreatedAtUtc = DateTime.UtcNow,
            IsShared = model.IsShared
        };

        _db.MediaClips.Add(clip);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(MyClips));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var clip = await _db.MediaClips.FindAsync(id);
        if (clip is null || clip.OwnerId != userId) return NotFound();

        var vm = new EditClipViewModel
        {
            Id = clip.Id,
            Title = clip.Title,
            Description = clip.Description,
            MediaType = clip.MediaType,
            IsShared = clip.IsShared
        };
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(EditClipViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var clip = await _db.MediaClips.FindAsync(model.Id);
        if (clip is null || clip.OwnerId != userId) return NotFound();

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
        var clip = await _db.MediaClips.FindAsync(id);
        if (clip is null || clip.OwnerId != userId) return NotFound();

        var filePath = Path.Combine(_env.WebRootPath, clip.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);

        _db.MediaClips.Remove(clip);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(MyClips));
    }
}
