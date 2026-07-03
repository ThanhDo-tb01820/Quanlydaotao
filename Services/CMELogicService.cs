using Microsoft.EntityFrameworkCore;
using CmeTracker.Api.Data;
using CmeTracker.Api.DTOs;
using CmeTracker.Api.Models;

namespace CmeTracker.Api.Services;

/// <summary>
/// Service xử lý nghiệp vụ CME:
/// - Tính tổng tiết thực tế (ActualHours, không phải TrainingHours)
/// - Xác định trạng thái chứng chỉ
/// - Kiểm tra minh chứng (file upload)
/// - Tạo danh sách cảnh báo
/// </summary>
public class CMELogicService
{
    private readonly CmeTrackerDbContext _db;
    private readonly IHttpContextAccessor _http;

    public CMELogicService(CmeTrackerDbContext db, IHttpContextAccessor http)
    {
        _db   = db;
        _http = http;
    }

    // ─── Cài đặt hệ thống ────────────────────────────────────
    public async Task<SystemSettingsDto> GetSettingsAsync()
    {
        var s = await _db.SystemSettings.ToListAsync();
        return new SystemSettingsDto
        {
            ExpiryWarningDays   = GetInt(s, "ExpiryWarningDays",   60),
            UrgentWarningDays   = GetInt(s, "UrgentWarningDays",   30),
            RequiredHours1Year  = GetInt(s, "RequiredHours1Year",  24),
            RequiredHours2Years = GetInt(s, "RequiredHours2Years", 48),
            RequiredHours5Years = GetInt(s, "RequiredHours5Years", 120),
        };
    }

    private static int GetInt(List<SystemSetting> list, string key, int def)
        => int.TryParse(list.FirstOrDefault(s => s.SettingKey == key)?.SettingValue, out var v) ? v : def;

    // ─── Trạng thái chứng chỉ ────────────────────────────────
    public (string Label, string CssClass, string BadgeClass, int DaysLeft) GetCertStatus(
        DateOnly? expiryDate, int warn30, int warn60)
    {
        if (expiryDate == null)
            return ("⚪ Chưa có hạn", "gray", "badge-gray", 9999);
        var today    = DateOnly.FromDateTime(DateTime.Today);
        int daysLeft = expiryDate.Value.DayNumber - today.DayNumber;

        if (daysLeft < 0)
            return ("🔴 Đã hết hạn",   "red",    "badge-red",    daysLeft);
        if (daysLeft <= warn30)
            return ("🟠 Sắp hết hạn",  "orange", "badge-orange", daysLeft);
        if (daysLeft <= warn60)
            return ("🟡 Cần theo dõi", "amber",  "badge-amber",  daysLeft);

        return ("🟢 Còn hiệu lực", "green", "badge-green", daysLeft);
    }

    // ─── Tổng tiết THỰC TẾ (dùng ActualHours) ────────────────
    /// <summary>
    /// Tính tổng số tiết thực tế đã học dựa vào ActualHours.
    /// Nếu ActualHours = 0 (chưa nhập), fallback về TrainingHours.
    /// </summary>
    public int GetTotalActualHours(IEnumerable<EmployeeTraining> trainings)
        => trainings.Sum(t => t.ActualHours > 0 ? t.ActualHours : t.TrainingHours);

    // ─── Trạng thái tuân thủ CME ─────────────────────────────
    public (bool Compliant, int TotalHours, int MissingHours) GetCompliance(
        IEnumerable<EmployeeTraining> trainings, int required)
    {
        var total   = GetTotalActualHours(trainings);
        var missing = Math.Max(0, required - total);
        return (total >= required, total, missing);
    }

    // ─── Build URL file minh chứng ───────────────────────────
    public string? BuildFileUrl(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return null;
        var req = _http.HttpContext?.Request;
        if (req == null) return null;
        return $"{req.Scheme}://{req.Host}/uploads/{fileName}";
    }

    // ─── Map Training → DTO ──────────────────────────────────
    public TrainingRecordDto MapTraining(EmployeeTraining t, int warn30, int warn60)
    {
        var (label, _, badge, days) = GetCertStatus(t.ExpiryDate, warn30, warn60);
        var actualH = t.ActualHours > 0 ? t.ActualHours : t.TrainingHours;
        return new TrainingRecordDto
        {
            TrainingId      = t.TrainingId,
            EmployeeId      = t.EmployeeId,
            EmployeeName    = t.Employee?.FullName ?? "",
            EmployeeCode    = t.Employee?.EmployeeCode ?? "",
            DepartmentName  = t.Employee?.Department?.DepartmentName ?? "",
            CourseId        = t.CourseId,
            CourseName      = t.Course?.CourseName ?? "",
            Organizer       = t.Course?.Organizer ?? "",
            TrainingHours   = t.TrainingHours,
            ActualHours     = actualH,
            IssueDate       = t.IssueDate?.ToString("yyyy-MM-dd") ?? "",
            ExpiryDate      = t.ExpiryDate?.ToString("yyyy-MM-dd") ?? "",
            Notes           = t.Notes,
            CertificateFile = t.CertificateFile,
            CertificateUrl  = BuildFileUrl(t.CertificateFile),
            HasEvidence     = !string.IsNullOrEmpty(t.CertificateFile),
            StatusLabel     = label,
            BadgeClass      = badge,
            DaysLeft        = days,
        };
    }

    // ─── Map Employee → DTO ──────────────────────────────────
    public EmployeeListDto MapEmployee(Employee emp, int required, int warn30, int warn60)
    {
        var (compliant, total, missing) = GetCompliance(emp.Trainings, required);
        var certWarnings = emp.Trainings.Count(t =>
        {
            var (_, css, _, _) = GetCertStatus(t.ExpiryDate, warn30, warn60);
            return css != "green";
        });
        // Số hồ sơ chưa có minh chứng
        var noEvidenceCount = emp.Trainings.Count(t => string.IsNullOrEmpty(t.CertificateFile));

        return new EmployeeListDto
        {
            EmployeeId      = emp.EmployeeId,
            EmployeeCode    = emp.EmployeeCode,
            FullName        = emp.FullName,
            Gender          = emp.Gender,
            DateOfBirth     = emp.DateOfBirth?.ToString("yyyy-MM-dd"),
            JoinDate        = emp.JoinDate?.ToString("yyyy-MM-dd"),
            DepartmentId    = emp.DepartmentId,
            DepartmentName  = emp.Department?.DepartmentName ?? "",
            Position        = emp.Position,
            IsActive        = emp.IsActive,
            TotalHours      = total,
            IsCompliant     = compliant,
            MissingHours    = missing,
            CertWarnings    = certWarnings,
            NoEvidenceCount = noEvidenceCount,
        };
    }

    // ─── Build danh sách cảnh báo ─────────────────────────────
    public async Task<List<AlertDto>> BuildAlertsAsync()
    {
        var cfg = await GetSettingsAsync();
        var employees = await _db.Employees
            .Where(e => e.IsActive)
            .Include(e => e.Department)
            .Include(e => e.Trainings).ThenInclude(t => t.Course)
            .ToListAsync();

        var alerts = new List<AlertDto>();

        foreach (var emp in employees)
        {
            // 1. Cảnh báo chứng chỉ hết hạn / sắp hết hạn
            foreach (var tr in emp.Trainings)
            {
                var (label, cssClass, badge, daysLeft) = GetCertStatus(
                    tr.ExpiryDate, cfg.UrgentWarningDays, cfg.ExpiryWarningDays);

                if (cssClass != "green")
                {
                    alerts.Add(new AlertDto
                    {
                        EmployeeCode = emp.EmployeeCode,
                        EmployeeName = emp.FullName,
                        Department   = emp.Department?.DepartmentName ?? "",
                        CourseName   = tr.Course?.CourseName ?? "",
                        ExpiryDate   = tr.ExpiryDate?.ToString("yyyy-MM-dd") ?? "",
                        DaysLeft     = daysLeft,
                        AlertType    = cssClass,
                        StatusLabel  = label,
                        BadgeClass   = badge,
                        AlertKind    = "cert",
                    });
                }

                // 2. Cảnh báo chưa có minh chứng
                if (string.IsNullOrEmpty(tr.CertificateFile))
                {
                    alerts.Add(new AlertDto
                    {
                        EmployeeCode = emp.EmployeeCode,
                        EmployeeName = emp.FullName,
                        Department   = emp.Department?.DepartmentName ?? "",
                        CourseName   = $"[{tr.Course?.CourseName}] — Chưa có minh chứng",
                        ExpiryDate   = tr.ExpiryDate?.ToString("yyyy-MM-dd") ?? "",
                        DaysLeft     = null,
                        AlertType    = "no-evidence",
                        StatusLabel  = "📎 Chưa có minh chứng",
                        BadgeClass   = "badge-gray-red",
                        AlertKind    = "no-evidence",
                    });
                }
            }

            // 3. Cảnh báo thiếu tiết CME
            var (compliant, total, missing) = GetCompliance(emp.Trainings, cfg.RequiredHours2Years);
            if (!compliant)
            {
                alerts.Add(new AlertDto
                {
                    EmployeeCode = emp.EmployeeCode,
                    EmployeeName = emp.FullName,
                    Department   = emp.Department?.DepartmentName ?? "",
                    CourseName   = $"Thiếu {missing} tiết CME ({total}/{cfg.RequiredHours2Years} tiết thực tế)",
                    ExpiryDate   = null,
                    DaysLeft     = null,
                    AlertType    = "missing",
                    StatusLabel  = "⚠️ Thiếu tiết CME",
                    BadgeClass   = "badge-amber",
                    AlertKind    = "missing",
                });
            }
        }

        // Sắp xếp: hết hạn → sắp hết → chưa minh chứng → thiếu tiết
        var order = new Dictionary<string, int>
        {
            ["red"] = 0, ["orange"] = 1, ["amber"] = 2,
            ["no-evidence"] = 3, ["missing"] = 4,
        };
        return alerts
            .OrderBy(a => order.GetValueOrDefault(a.AlertType, 5))
            .ThenBy(a => a.DaysLeft ?? 9999)
            .ToList();
    }
}
