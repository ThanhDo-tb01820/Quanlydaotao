using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CmeTracker.Api.Data;
using CmeTracker.Api.DTOs;

namespace CmeTracker.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Admin")]
public class AuditLogController : ControllerBase
{
    private readonly CmeTrackerDbContext _db;

    public AuditLogController(CmeTrackerDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<AuditLogDto>>> GetLogs()
    {
        var logs = await _db.AuditLogs
            .OrderByDescending(l => l.CreatedAt)
            .Take(100)
            .Select(l => new AuditLogDto
            {
                AuditLogId = l.AuditLogId,
                UserId = l.UserId,
                Username = l.Username,
                Action = l.Action,
                Description = l.Description,
                CreatedAt = l.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss")
            })
            .ToListAsync();

        return Ok(logs);
    }
}
