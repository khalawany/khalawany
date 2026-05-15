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

    // ── Watch ──────────────────────────────────────────────────────────────

    public async Task<IActionResult> Watch(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var clip = await _db.MediaClips
            .Include(c => c.Owner)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (clip is null || clip.IsBlocked) return NotFound();

        // Only owner or shared clips are visible
        if (clip.OwnerId != userId && !clip.IsShared) return NotFound();

        var comments = await _db.Comments
            .Include(c => c.User)
            .Where(c => c.MediaClipId == id && !c.IsDeleted)
            .OrderBy(c => c.CreatedAtUtc)
            .ToListAsync();

        var likeCount = await _db.Likes.CountAsync(l => l.MediaClipId == id);
        var userHasLiked = await _db.Likes.AnyAsync(l => l.MediaClipId == id && l.UserId == userId);

        var vm = new WatchViewModel
        {
            Clip = clip,
            Comments = comments,
            LikeCount = likeCount,
            UserHasLiked = userHasLiked,
            IsOwner = clip.OwnerId == userId
        };

        return View(vm);
    }

    // ── Like (AJAX) ────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> ToggleLike(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var clip = await _db.MediaClips.FindAsync(id);
        if (clip is null || clip.IsBlocked) return NotFound();

        var existing = await _db.Likes.FirstOrDefaultAsync(l => l.MediaClipId == id && l.UserId == userId);
        bool liked;
        if (existing is not null)
        {
            _db.Likes.Remove(existing);
            liked = false;
        }
        else
        {
            _db.Likes.Add(new Like { MediaClipId = id, UserId = userId, CreatedAtUtc = DateTime.UtcNow });
            liked = true;
        }

        await _db.SaveChangesAsync();
        var count = await _db.Likes.CountAsync(l => l.MediaClipId == id);
        return Json(new { liked, count });
    }

    // ── Comments ───────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> AddComment(int id, string content)
    {
        if (string.IsNullOrWhiteSpace(content) || content.Length > 1000)
            return RedirectToAction(nameof(Watch), new { id });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var clip = await _db.MediaClips.FindAsync(id);
        if (clip is null || clip.IsBlocked) return NotFound();
        if (clip.OwnerId != userId && !clip.IsShared) return NotFound();

        _db.Comments.Add(new Comment
        {
            MediaClipId = id,
            UserId = userId,
            Content = content.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Watch), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> RequestCommentDelete(int commentId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var comment = await _db.Comments.FindAsync(commentId);
        if (comment is null || comment.UserId != userId) return NotFound();

        comment.IsDeleteRequested = true;
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Watch), new { id = comment.MediaClipId });
    }
}
