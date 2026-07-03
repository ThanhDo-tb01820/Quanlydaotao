using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CmeTracker.Api.Data;
using CmeTracker.Api.Models;

namespace CmeTracker.Api.Controllers;

/// <summary>
/// Upload file minh chứng (ảnh điểm danh, scan chứng chỉ, PDF)
/// POST /api/v1/upload/training/{trainingId}  — Upload file
/// DELETE /api/v1/upload/training/{trainingId} — Xóa file
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class UploadController : ControllerBase
{
    private readonly CmeTrackerDbContext _db;
    private readonly IWebHostEnvironment _env;

    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".pdf", ".webp"];
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public UploadController(CmeTrackerDbContext db, IWebHostEnvironment env)
    {
        _db  = db;
        _env = env;
    }

    /// <summary>
    /// POST /api/v1/upload/training/{trainingId}
    /// Upload file minh chứng cho một bản ghi đào tạo
    /// </summary>
    [HttpPost("training/{trainingId}")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> UploadEvidence(int trainingId, IFormFile file)
    {
        var record = await _db.EmployeeTrainings.FindAsync(trainingId);
        if (record == null)
            return NotFound(new { message = "Không tìm thấy bản ghi đào tạo!" });

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Vui lòng chọn file!" });

        if (file.Length > MaxFileSizeBytes)
            return BadRequest(new { message = "File quá lớn! Tối đa 10MB." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return BadRequest(new { message = $"Chỉ chấp nhận: {string.Join(", ", AllowedExtensions)}" });

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
        Directory.CreateDirectory(uploadsDir);

        if (!string.IsNullOrEmpty(record.CertificateFile))
        {
            var oldPath = Path.Combine(uploadsDir, record.CertificateFile);
            if (System.IO.File.Exists(oldPath))
                System.IO.File.Delete(oldPath);
        }

        var newFileName = $"tr_{trainingId}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{ext}";
        var savePath    = Path.Combine(uploadsDir, newFileName);

        await using (var stream = new FileStream(savePath, FileMode.Create))
            await file.CopyToAsync(stream);

        record.CertificateFile = newFileName;

        var currentUser = User.Identity?.Name ?? "Unknown";
        _db.AuditLogs.Add(new AuditLog
        {
            Username = currentUser,
            Action = "Upload Certificate",
            Description = $"Uploaded file: {newFileName} for training record ID {trainingId}",
            CreatedAt = DateTime.Now
        });

        await _db.SaveChangesAsync();

        var fileUrl = $"{Request.Scheme}://{Request.Host}/uploads/{newFileName}";
        return Ok(new
        {
            message         = "✅ Upload minh chứng thành công!",
            fileName        = newFileName,
            certificateUrl  = fileUrl,
            hasEvidence     = true,
        });
    }

    /// <summary>
    /// DELETE /api/v1/upload/training/{trainingId}
    /// Xóa file minh chứng của một bản ghi
    /// </summary>
    [HttpDelete("training/{trainingId}")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> DeleteEvidence(int trainingId)
    {
        var record = await _db.EmployeeTrainings.FindAsync(trainingId);
        if (record == null) return NotFound();

        if (!string.IsNullOrEmpty(record.CertificateFile))
        {
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
            var filePath   = Path.Combine(uploadsDir, record.CertificateFile);
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }

        var oldFileName = record.CertificateFile;
        record.CertificateFile = null;

        var currentUser = User.Identity?.Name ?? "Unknown";
        _db.AuditLogs.Add(new AuditLog
        {
            Username = currentUser,
            Action = "Delete Certificate Evidence",
            Description = $"Deleted file evidence ({oldFileName}) for training record ID {trainingId}",
            CreatedAt = DateTime.Now
        });

        await _db.SaveChangesAsync();
        return NoContent();
    }
}
