using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CmeTracker.Api.Data;
using CmeTracker.Api.DTOs;
using CmeTracker.Api.Services;
using CmeTracker.Api.Models;

namespace CmeTracker.Api.Controllers;

// ═══════════════════════════════════════════════════════════════
//  DASHBOARD CONTROLLER
// ═══════════════════════════════════════════════════════════════
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly CmeTrackerDbContext _db;
    private readonly CMELogicService _cme;

    public DashboardController(CmeTrackerDbContext db, CMELogicService cme)
    {
        _db  = db;
        _cme = cme;
    }

    /// <summary>GET /api/v1/dashboard/summary — Thống kê tổng quan</summary>
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary()
    {
        var cfg = await _cme.GetSettingsAsync();
        var employees = await _db.Employees
            .Where(e => e.IsActive)
            .Include(e => e.Trainings)
            .ToListAsync();

        var allTrainings = employees.SelectMany(e => e.Trainings).ToList();

        int compliant    = employees.Count(e =>
            _cme.GetCompliance(e.Trainings, cfg.RequiredHours2Years).Compliant);
        int expired      = allTrainings.Count(t =>
            _cme.GetCertStatus(t.ExpiryDate, cfg.UrgentWarningDays, cfg.ExpiryWarningDays).CssClass == "red");
        int urgentExp    = allTrainings.Count(t =>
            _cme.GetCertStatus(t.ExpiryDate, cfg.UrgentWarningDays, cfg.ExpiryWarningDays).CssClass == "orange");
        int watchExp     = allTrainings.Count(t =>
            _cme.GetCertStatus(t.ExpiryDate, cfg.UrgentWarningDays, cfg.ExpiryWarningDays).CssClass == "amber");

        return Ok(new DashboardSummaryDto
        {
            TotalEmployees       = employees.Count,
            CompliantEmployees   = compliant,
            NonCompliantEmployees= employees.Count - compliant,
            ExpiredCertificates  = expired,
            UrgentCertificates   = urgentExp,
            ExpiringCertificates = urgentExp + watchExp,
        });
    }

    /// <summary>GET /api/v1/dashboard/alerts — Danh sách cảnh báo</summary>
    [HttpGet("alerts")]
    public async Task<ActionResult<List<AlertDto>>> GetAlerts([FromQuery] string? type = null)
    {
        var alerts = await _cme.BuildAlertsAsync();
        if (!string.IsNullOrEmpty(type))
            alerts = alerts.Where(a => a.AlertType == type || a.AlertKind == type).ToList();
        return Ok(alerts);
    }
}

// ═══════════════════════════════════════════════════════════════
//  EMPLOYEES CONTROLLER
// ═══════════════════════════════════════════════════════════════
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly CmeTrackerDbContext _db;
    private readonly CMELogicService _cme;

    public EmployeesController(CmeTrackerDbContext db, CMELogicService cme)
    {
        _db  = db;
        _cme = cme;
    }

    private async Task LogAuditAsync(string action, string description)
    {
        var username = User.Identity?.Name ?? "Unknown";
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        int? userId = int.TryParse(userIdClaim, out var parsed) ? parsed : null;
        
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Username = username,
            Action = action,
            Description = description,
            CreatedAt = DateTime.Now
        });
        await _db.SaveChangesAsync();
    }

    /// <summary>GET /api/v1/employees — Danh sách nhân viên (có filter)</summary>
    [HttpGet]
    public async Task<ActionResult<List<EmployeeListDto>>> GetAll(
        [FromQuery] string? search     = null,
        [FromQuery] int?    deptId     = null,
        [FromQuery] string? compliance = null)
    {
        var cfg = await _cme.GetSettingsAsync();

        var query = _db.Employees
            .Where(e => e.IsActive)
            .Include(e => e.Department)
            .Include(e => e.Trainings).ThenInclude(t => t.Course)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(e =>
                e.FullName.Contains(search) || e.EmployeeCode.Contains(search));

        if (deptId.HasValue)
            query = query.Where(e => e.DepartmentId == deptId.Value);

        var list = await query.ToListAsync();

        var dtos = list.Select(e => _cme.MapEmployee(
            e, cfg.RequiredHours2Years, cfg.UrgentWarningDays, cfg.ExpiryWarningDays)).ToList();

        if (compliance == "compliant")
            dtos = dtos.Where(d => d.IsCompliant).ToList();
        else if (compliance == "non-compliant")
            dtos = dtos.Where(d => !d.IsCompliant).ToList();

        return Ok(dtos);
    }

    /// <summary>GET /api/v1/employees/{id} — Chi tiết nhân viên + lịch sử chứng chỉ</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<EmployeeDetailDto>> GetById(int id)
    {
        var cfg = await _cme.GetSettingsAsync();
        var emp = await _db.Employees
            .Include(e => e.Department)
            .Include(e => e.Trainings).ThenInclude(t => t.Course)
            .FirstOrDefaultAsync(e => e.EmployeeId == id);

        if (emp == null) return NotFound();

        var base_ = _cme.MapEmployee(emp, cfg.RequiredHours2Years, cfg.UrgentWarningDays, cfg.ExpiryWarningDays);
        var detail = new EmployeeDetailDto
        {
            EmployeeId     = base_.EmployeeId,
            EmployeeCode   = base_.EmployeeCode,
            FullName       = base_.FullName,
            Gender         = base_.Gender,
            DateOfBirth    = base_.DateOfBirth,
            JoinDate       = base_.JoinDate,
            DepartmentId   = base_.DepartmentId,
            DepartmentName = base_.DepartmentName,
            Position       = base_.Position,
            IsActive       = base_.IsActive,
            TotalHours     = base_.TotalHours,
            IsCompliant    = base_.IsCompliant,
            MissingHours   = base_.MissingHours,
            CertWarnings   = base_.CertWarnings,
            Trainings      = emp.Trainings
                .Select(t => _cme.MapTraining(t, cfg.UrgentWarningDays, cfg.ExpiryWarningDays))
                .ToList(),
        };

        return Ok(detail);
    }

    /// <summary>POST /api/v1/employees — Thêm nhân viên mới</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,HR")]
    public async Task<ActionResult<EmployeeListDto>> Create([FromBody] CreateEmployeeDto dto)
    {
        if (await _db.Employees.AnyAsync(e => e.EmployeeCode == dto.EmployeeCode))
            return Conflict(new { message = "Mã nhân viên đã tồn tại!" });

        var emp = new Models.Employee
        {
            EmployeeCode = dto.EmployeeCode,
            FullName     = dto.FullName,
            Gender       = dto.Gender,
            DateOfBirth  = dto.DateOfBirth != null ? DateOnly.Parse(dto.DateOfBirth) : null,
            Email        = dto.Email,
            Phone        = dto.Phone,
            DepartmentId = dto.DepartmentId,
            Position     = dto.Position,
            JoinDate     = dto.JoinDate != null ? DateOnly.Parse(dto.JoinDate) : null,
            IsActive     = true,
        };

        _db.Employees.Add(emp);
        await _db.SaveChangesAsync();

        await LogAuditAsync("Create Employee", $"Created employee: {emp.FullName} (Code: {emp.EmployeeCode}, Position: {emp.Position})");

        var created = await _db.Employees
            .Include(e => e.Department)
            .Include(e => e.Trainings)
            .FirstAsync(e => e.EmployeeId == emp.EmployeeId);

        var cfg = await _cme.GetSettingsAsync();
        return CreatedAtAction(nameof(GetById), new { id = emp.EmployeeId },
            _cme.MapEmployee(created, cfg.RequiredHours2Years, cfg.UrgentWarningDays, cfg.ExpiryWarningDays));
    }

    /// <summary>PUT /api/v1/employees/{id} — Cập nhật nhân viên</summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateEmployeeDto dto)
    {
        var emp = await _db.Employees.FindAsync(id);
        if (emp == null) return NotFound();

        emp.FullName     = dto.FullName;
        emp.Gender       = dto.Gender;
        emp.DateOfBirth  = dto.DateOfBirth != null ? DateOnly.Parse(dto.DateOfBirth) : null;
        emp.Email        = dto.Email;
        emp.Phone        = dto.Phone;
        emp.DepartmentId = dto.DepartmentId;
        emp.Position     = dto.Position;
        emp.JoinDate     = dto.JoinDate != null ? DateOnly.Parse(dto.JoinDate) : null;

        await _db.SaveChangesAsync();
        await LogAuditAsync("Update Employee", $"Updated employee: {emp.FullName} (Code: {emp.EmployeeCode})");

        return NoContent();
    }

    /// <summary>DELETE /api/v1/employees/{id} — Xóa mềm nhân viên</summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> Delete(int id)
    {
        var emp = await _db.Employees.FindAsync(id);
        if (emp == null) return NotFound();
        
        emp.IsDeleted = true;
        emp.DeletedAt = DateTime.Now;
        
        await _db.SaveChangesAsync();
        await LogAuditAsync("Delete Employee (Soft)", $"Soft deleted employee: {emp.FullName} (Code: {emp.EmployeeCode})");
        
        return NoContent();
    }
}

// ═══════════════════════════════════════════════════════════════
//  TRAININGS CONTROLLER
// ═══════════════════════════════════════════════════════════════
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class TrainingsController : ControllerBase
{
    private readonly CmeTrackerDbContext _db;
    private readonly CMELogicService _cme;

    public TrainingsController(CmeTrackerDbContext db, CMELogicService cme)
    {
        _db  = db;
        _cme = cme;
    }

    private async Task LogAuditAsync(string action, string description)
    {
        var username = User.Identity?.Name ?? "Unknown";
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        int? userId = int.TryParse(userIdClaim, out var parsed) ? parsed : null;
        
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Username = username,
            Action = action,
            Description = description,
            CreatedAt = DateTime.Now
        });
        await _db.SaveChangesAsync();
    }

    /// <summary>GET /api/v1/trainings — Danh sách chứng chỉ (có filter)</summary>
    [HttpGet]
    public async Task<ActionResult<List<TrainingRecordDto>>> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] string? status = null)
    {
        var cfg = await _cme.GetSettingsAsync();

        var list = await _db.EmployeeTrainings
            .Include(t => t.Employee).ThenInclude(e => e.Department)
            .Include(t => t.Course)
            .ToListAsync();

        if (!string.IsNullOrEmpty(search))
        {
            var s = search.ToLower();
            list = list.Where(t =>
                (t.Course?.CourseName.ToLower().Contains(s) ?? false) ||
                (t.Employee?.FullName.ToLower().Contains(s) ?? false)).ToList();
        }

        var dtos = list.Select(t => _cme.MapTraining(t, cfg.UrgentWarningDays, cfg.ExpiryWarningDays)).ToList();

        if (!string.IsNullOrEmpty(status))
        {
            dtos = status switch
            {
                "expired"    => dtos.Where(d => d.BadgeClass == "badge-red").ToList(),
                "expiring30" => dtos.Where(d => d.BadgeClass == "badge-orange").ToList(),
                "expiring60" => dtos.Where(d => d.BadgeClass == "badge-amber").ToList(),
                "valid"      => dtos.Where(d => d.BadgeClass == "badge-green").ToList(),
                _ => dtos
            };
        }

        return Ok(dtos);
    }

    /// <summary>POST /api/v1/trainings — Thêm chứng chỉ đào tạo</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,HR")]
    public async Task<ActionResult<TrainingRecordDto>> Create([FromBody] CreateTrainingDto dto)
    {
        int courseId = dto.CourseId;
        if (courseId == 0 && !string.IsNullOrEmpty(dto.CourseName))
        {
            var newCourse = new Models.TrainingCourse
            {
                CourseCode    = "CUSTOM",
                CourseName    = dto.CourseName,
                Organizer     = dto.Organizer,
                DefaultHours  = dto.TrainingHours,
            };
            _db.TrainingCourses.Add(newCourse);
            await _db.SaveChangesAsync();
            courseId = newCourse.CourseId;
        }

        var record = new Models.EmployeeTraining
        {
            EmployeeId    = dto.EmployeeId,
            CourseId      = courseId,
            TrainingHours = dto.TrainingHours,
            ActualHours   = dto.ActualHours,
            IssueDate     = DateOnly.Parse(dto.IssueDate),
            ExpiryDate    = DateOnly.Parse(dto.ExpiryDate),
            Notes         = dto.Notes,
        };

        _db.EmployeeTrainings.Add(record);
        await _db.SaveChangesAsync();

        var created = await _db.EmployeeTrainings
            .Include(t => t.Employee).ThenInclude(e => e.Department)
            .Include(t => t.Course)
            .FirstAsync(t => t.TrainingId == record.TrainingId);

        await LogAuditAsync("Add Certificate", $"Added certificate '{created.Course.CourseName}' for employee ID {record.EmployeeId}");

        var cfg = await _cme.GetSettingsAsync();
        return Created("", _cme.MapTraining(created, cfg.UrgentWarningDays, cfg.ExpiryWarningDays));
    }

    /// <summary>DELETE /api/v1/trainings/{id} — Xóa chứng chỉ</summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<IActionResult> Delete(int id)
    {
        var t = await _db.EmployeeTrainings.Include(x => x.Course).FirstOrDefaultAsync(x => x.TrainingId == id);
        if (t == null) return NotFound();

        var courseName = t.Course?.CourseName ?? "Unknown Course";
        _db.EmployeeTrainings.Remove(t);
        await _db.SaveChangesAsync();

        await LogAuditAsync("Delete Certificate", $"Deleted certificate '{courseName}' (Record ID: {id})");
        return NoContent();
    }
}

// ═══════════════════════════════════════════════════════════════
//  SETTINGS CONTROLLER
// ═══════════════════════════════════════════════════════════════
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly CmeTrackerDbContext _db;
    private readonly CMELogicService _cme;

    public SettingsController(CmeTrackerDbContext db, CMELogicService cme)
    {
        _db  = db;
        _cme = cme;
    }

    private async Task LogAuditAsync(string action, string description)
    {
        var username = User.Identity?.Name ?? "Unknown";
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        int? userId = int.TryParse(userIdClaim, out var parsed) ? parsed : null;
        
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Username = username,
            Action = action,
            Description = description,
            CreatedAt = DateTime.Now
        });
        await _db.SaveChangesAsync();
    }

    /// <summary>GET /api/v1/settings — Lấy toàn bộ cài đặt</summary>
    [HttpGet]
    public async Task<ActionResult<SystemSettingsDto>> Get()
        => Ok(await _cme.GetSettingsAsync());

    /// <summary>PUT /api/v1/settings — Lưu cài đặt</summary>
    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update([FromBody] SystemSettingsDto dto)
    {
        await UpsertSetting("ExpiryWarningDays",   dto.ExpiryWarningDays.ToString());
        await UpsertSetting("UrgentWarningDays",   dto.UrgentWarningDays.ToString());
        await UpsertSetting("RequiredHours1Year",  dto.RequiredHours1Year.ToString());
        await UpsertSetting("RequiredHours2Years", dto.RequiredHours2Years.ToString());
        await UpsertSetting("RequiredHours5Years", dto.RequiredHours5Years.ToString());
        await _db.SaveChangesAsync();

        await LogAuditAsync("Update Settings", "Updated system warning thresholds and required CME hours");
        return NoContent();
    }

    private async Task UpsertSetting(string key, string value)
    {
        var s = await _db.SystemSettings.FindAsync(key);
        if (s == null) _db.SystemSettings.Add(new Models.SystemSetting { SettingKey = key, SettingValue = value });
        else s.SettingValue = value;
    }
}

// ═══════════════════════════════════════════════════════════════
//  DEPARTMENTS & COURSES CONTROLLER
// ═══════════════════════════════════════════════════════════════
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly CmeTrackerDbContext _db;
    public DepartmentsController(CmeTrackerDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<DepartmentDto>>> GetAll()
    {
        var depts = await _db.Departments
            .Include(d => d.Employees)
            .ToListAsync();
        return Ok(depts.Select(d => new DepartmentDto
        {
            DepartmentId   = d.DepartmentId,
            DepartmentCode = d.DepartmentCode,
            DepartmentName = d.DepartmentName,
            EmployeeCount  = d.Employees.Count(e => e.IsActive),
        }));
    }
}

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class CoursesController : ControllerBase
{
    private readonly CmeTrackerDbContext _db;
    public CoursesController(CmeTrackerDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<TrainingCourseDto>>> GetAll()
    {
        var courses = await _db.TrainingCourses.ToListAsync();
        return Ok(courses.Select(c => new TrainingCourseDto
        {
            CourseId     = c.CourseId,
            CourseCode   = c.CourseCode,
            CourseName   = c.CourseName,
            Organizer    = c.Organizer,
            DefaultHours = c.DefaultHours,
        }));
    }
}
