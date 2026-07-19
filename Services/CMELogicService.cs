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

    // ─── Trạng thái chứng chỉ ───────────────────────────────────
    public (string Label, string CssClass, string BadgeClass, int DaysLeft) GetCertStatus(
        DateOnly? expiryDate, int warn30, int warn60, bool isCompleted = false, bool isLifetime = false)
    {
        // Chứng chỉ vĩnh viễn → luôn hợp lệ, không cảnh báo
        if (isLifetime)
            return ("♾️ Vĩnh viễn", "green", "badge-lifetime", 99999);

        if (isCompleted)
            return ("✅ Hoàn thành", "green", "badge-green", 9999);

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
    {
        var twoYearsAgo = DateOnly.FromDateTime(DateTime.Today.AddYears(-2));
        return trainings
            .Where(t => t.IssueDate >= twoYearsAgo)
            .Sum(t => t.ActualHours > 0 ? t.ActualHours : t.TrainingHours);
    }

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

    // ─── Map Training → DTO ──────────────────────────────────────────
    public TrainingRecordDto MapTraining(EmployeeTraining t, int warn30, int warn60)
    {
        var actualH = t.ActualHours > 0 ? t.ActualHours : t.TrainingHours;
        bool isCompleted = actualH >= t.TrainingHours && t.TrainingHours > 0;
        bool isLifetime  = t.Course?.IsLifetime ?? false;
        bool needsRenewal = !isLifetime && (t.Course?.RequiresRenewalAfterYears ?? 0) > 0;
        int? renewalYears = needsRenewal ? t.Course?.RequiresRenewalAfterYears : null;

        // Nếu khóa học có yêu cầu học lại và chưa có ExpiryDate, tính tự động
        var effectiveExpiry = t.ExpiryDate;
        if (effectiveExpiry == null && needsRenewal && t.IssueDate.HasValue && renewalYears.HasValue)
            effectiveExpiry = t.IssueDate.Value.AddYears(renewalYears.Value);

        var (label, _, badge, days) = GetCertStatus(effectiveExpiry, warn30, warn60, isCompleted, isLifetime);

        string certTypeLabel = isLifetime ? "Vĩnh viễn" : (needsRenewal ? $"Hạn {renewalYears} năm" : "Có thời hạn");
        string certTypeBadge = isLifetime ? "badge-lifetime" : "badge-expiry";

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
            ExpiryDate      = effectiveExpiry?.ToString("yyyy-MM-dd") ?? "",
            Notes           = t.Notes,
            CertificateFile = t.CertificateFile,
            CertificateUrl  = BuildFileUrl(t.CertificateFile),
            HasEvidence     = !string.IsNullOrEmpty(t.CertificateFile),
            StatusLabel     = label,
            BadgeClass      = badge,
            DaysLeft        = days,
            IsLifetime      = isLifetime,
            NeedsRenewal    = needsRenewal,
            RenewalAfterYears = renewalYears,
            CertTypeLabel   = certTypeLabel,
            CertTypeBadge   = certTypeBadge,
        };
    }

    // ─── Map Employee → DTO ──────────────────────────────────
    public EmployeeListDto MapEmployee(Employee emp, int required, int warn30, int warn60)
    {
        var (compliant, total, missing) = GetCompliance(emp.Trainings, required);
        var certWarnings = emp.Trainings.Count(t =>
        {
            bool ltm = t.Course?.IsLifetime ?? false;
            if (ltm) return false; // vĩnh viễn không cảnh báo
            var actualH = t.ActualHours > 0 ? t.ActualHours : t.TrainingHours;
            bool isCompleted = actualH >= t.TrainingHours && t.TrainingHours > 0;
            var effectiveExp = t.ExpiryDate;
            bool needsRen = !ltm && (t.Course?.RequiresRenewalAfterYears ?? 0) > 0;
            if (effectiveExp == null && needsRen && t.IssueDate.HasValue)
                effectiveExp = t.IssueDate.Value.AddYears(t.Course!.RequiresRenewalAfterYears!.Value);
            var (_, css, _, _) = GetCertStatus(effectiveExp, warn30, warn60, isCompleted, ltm);
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
                bool isLifetime = tr.Course?.IsLifetime ?? false;
                bool needsRenewal = !isLifetime && (tr.Course?.RequiresRenewalAfterYears ?? 0) > 0;
                int? renewalYears = needsRenewal ? tr.Course?.RequiresRenewalAfterYears : null;

                // Chứng chỉ vĩnh viễn: bỏ qua hoàn toàn
                if (isLifetime) continue;

                // Tính ExpiryDate hiệu quả (tự động nếu có yêu cầu học lại)
                var effectiveExpiry = tr.ExpiryDate;
                if (effectiveExpiry == null && needsRenewal && tr.IssueDate.HasValue && renewalYears.HasValue)
                    effectiveExpiry = tr.IssueDate.Value.AddYears(renewalYears.Value);

                var actualH = tr.ActualHours > 0 ? tr.ActualHours : tr.TrainingHours;
                bool isCompleted = actualH >= tr.TrainingHours && tr.TrainingHours > 0;
                var (label, cssClass, badge, daysLeft) = GetCertStatus(
                    effectiveExpiry, cfg.UrgentWarningDays, cfg.ExpiryWarningDays, isCompleted, false);

                if (cssClass != "green")
                {
                    alerts.Add(new AlertDto
                    {
                        EmployeeCode  = emp.EmployeeCode,
                        EmployeeName  = emp.FullName,
                        EmployeeId    = emp.EmployeeId,
                        Department    = emp.Department?.DepartmentName ?? "",
                        CourseName    = tr.Course?.CourseName ?? "",
                        IssueDate     = tr.IssueDate?.ToString("yyyy-MM-dd"),
                        ExpiryDate    = effectiveExpiry?.ToString("yyyy-MM-dd") ?? "",
                        DaysLeft      = daysLeft,
                        AlertType     = cssClass,
                        StatusLabel   = label,
                        BadgeClass    = badge,
                        AlertKind     = "cert",
                        IsLifetime    = false,
                        NeedsRenewal  = needsRenewal,
                        RenewalAfterYears = renewalYears,
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

    // ─── Build thống kê theo phòng ban ────────────────────────────────
    public async Task<List<DepartmentStatsDto>> BuildDepartmentStatsAsync()
    {
        var cfg = await GetSettingsAsync();
        var employees = await _db.Employees
            .Where(e => e.IsActive)
            .Include(e => e.Department)
            .Include(e => e.Trainings).ThenInclude(t => t.Course)
            .ToListAsync();

        var grouped = employees.GroupBy(e => new { e.DepartmentId, Name = e.Department?.DepartmentName ?? "" });
        var result  = new List<DepartmentStatsDto>();

        foreach (var g in grouped.OrderBy(g => g.Key.Name))
        {
            var empList      = g.ToList();
            int compliant    = 0;
            int nonCompliant = 0;
            int expiredCerts = 0;
            int expiringCerts= 0;
            var empSummaries = new List<EmployeeCertSummaryDto>();

            foreach (var emp in empList)
            {
                var (comp, total, missing) = GetCompliance(emp.Trainings, cfg.RequiredHours2Years);
                if (comp) compliant++; else nonCompliant++;

                int expiredE  = 0;
                int expiringE = 0;
                var certList  = new List<CertBriefDto>();

                foreach (var tr in emp.Trainings)
                {
                    bool isLtm   = tr.Course?.IsLifetime ?? false;
                    bool needsRen= !isLtm && (tr.Course?.RequiresRenewalAfterYears ?? 0) > 0;
                    var effectiveExp = tr.ExpiryDate;
                    if (effectiveExp == null && needsRen && tr.IssueDate.HasValue)
                        effectiveExp = tr.IssueDate.Value.AddYears(tr.Course!.RequiresRenewalAfterYears!.Value);

                    var actualH  = tr.ActualHours > 0 ? tr.ActualHours : tr.TrainingHours;
                    bool isDone  = actualH >= tr.TrainingHours && tr.TrainingHours > 0;
                    var (lbl, css, badge, days) = GetCertStatus(
                        effectiveExp, cfg.UrgentWarningDays, cfg.ExpiryWarningDays, isDone, isLtm);

                    if (!isLtm)
                    {
                        if (css == "red")    { expiredE++; expiredCerts++; }
                        if (css == "orange" || css == "amber") { expiringE++; expiringCerts++; }
                    }

                    certList.Add(new CertBriefDto
                    {
                        TrainingId        = tr.TrainingId,
                        CourseName        = tr.Course?.CourseName ?? "",
                        IssueDate         = tr.IssueDate?.ToString("yyyy-MM-dd") ?? "",
                        ExpiryDate        = effectiveExp?.ToString("yyyy-MM-dd") ?? "",
                        IsLifetime        = isLtm,
                        NeedsRenewal      = needsRen,
                        RenewalAfterYears = needsRen ? tr.Course?.RequiresRenewalAfterYears : null,
                        StatusLabel       = lbl,
                        BadgeClass        = badge,
                        DaysLeft          = days,
                    });
                }

                string statusLvl = comp ? "green" : (expiredE > 0 ? "red" : expiringE > 0 ? "orange" : "amber");
                empSummaries.Add(new EmployeeCertSummaryDto
                {
                    EmployeeId     = emp.EmployeeId,
                    EmployeeCode   = emp.EmployeeCode,
                    FullName       = emp.FullName,
                    Position       = emp.Position,
                    DepartmentName = emp.Department?.DepartmentName ?? "",
                    IsCompliant    = comp,
                    TotalHours     = total,
                    MissingHours   = missing,
                    ExpiredCerts   = expiredE,
                    ExpiringCerts  = expiringE,
                    StatusLevel    = statusLvl,
                    Certificates   = certList,
                });
            }

            int pct = empList.Count > 0 ? (compliant * 100 / empList.Count) : 100;
            string alertLevel = nonCompliant == 0 ? "green" :
                                expiredCerts  > 0 ? "red"   :
                                expiringCerts > 0 ? "orange": "amber";

            result.Add(new DepartmentStatsDto
            {
                DepartmentId          = g.Key.DepartmentId,
                DepartmentName        = g.Key.Name,
                TotalEmployees        = empList.Count,
                CompliantEmployees    = compliant,
                NonCompliantEmployees = nonCompliant,
                ExpiredCertificates   = expiredCerts,
                ExpiringCertificates  = expiringCerts,
                CompliancePercent     = pct,
                AlertLevel            = alertLevel,
                Employees             = empSummaries
                    .OrderBy(e => e.IsCompliant)
                    .ThenByDescending(e => e.ExpiredCerts)
                    .ToList(),
            });
        }
        return result.OrderBy(d => d.CompliancePercent).ToList();
    }
}
