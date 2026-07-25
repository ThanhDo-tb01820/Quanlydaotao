using System.ComponentModel.DataAnnotations;

namespace CmeTracker.Api.DTOs;

// ─── Dashboard ──────────────────────────────────────────────
public class DashboardSummaryDto
{
    public int TotalEmployees { get; set; }
    public int CompliantEmployees { get; set; }       // Đạt CME
    public int NonCompliantEmployees { get; set; }    // Chưa đạt CME
    public int ExpiringCertificates { get; set; }     // Sắp hết hạn (≤60 ngày)
    public int ExpiredCertificates { get; set; }      // Đã hết hạn
    public int UrgentCertificates { get; set; }       // Khẩn cấp (≤30 ngày)
}

// ─── Thống kê theo Phòng ban / Chuyên môn ────────────────────
public class DepartmentStatsDto
{
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = "";
    public int TotalEmployees { get; set; }
    public int CompliantEmployees { get; set; }
    public int NonCompliantEmployees { get; set; }
    public int ExpiringCertificates { get; set; }      // Sắp hết hạn
    public int ExpiredCertificates { get; set; }       // Đã hết hạn
    public int CompliancePercent { get; set; }         // % đạt
    public string AlertLevel { get; set; } = "green";  // green | amber | orange | red
    public List<EmployeeCertSummaryDto> Employees { get; set; } = new();
}

public class EmployeeCertSummaryDto
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Position { get; set; } = "";
    public string DepartmentName { get; set; } = "";
    public bool IsCompliant { get; set; }
    public int TotalHours { get; set; }
    public int MissingHours { get; set; }
    public int ExpiredCerts { get; set; }
    public int ExpiringCerts { get; set; }
    public string StatusLevel { get; set; } = "green";  // green | amber | orange | red
    public List<CertBriefDto> Certificates { get; set; } = new();
}

public class CertBriefDto
{
    public int TrainingId { get; set; }
    public string CourseName { get; set; } = "";
    public string IssueDate { get; set; } = "";
    public string ExpiryDate { get; set; } = "";
    public bool IsLifetime { get; set; }
    public bool NeedsRenewal { get; set; }
    public int? RenewalAfterYears { get; set; }
    public string StatusLabel { get; set; } = "";
    public string BadgeClass { get; set; } = "";
    public int DaysLeft { get; set; }
}

public class AlertDto
{
    public string EmployeeCode { get; set; } = "";
    public string EmployeeName { get; set; } = "";
    public int EmployeeId { get; set; }
    public string Department { get; set; } = "";
    public string CourseName { get; set; } = "";
    public string? IssueDate { get; set; }             // Ngày cấp
    public string? ExpiryDate { get; set; }            // Ngày hết hạn
    public int? DaysLeft { get; set; }
    public string AlertType { get; set; } = "";        // expired | orange | amber | missing
    public string StatusLabel { get; set; } = "";
    public string BadgeClass { get; set; } = "";
    public string AlertKind { get; set; } = "";        // cert | missing
    public bool IsLifetime { get; set; }               // Chứng chỉ vĩnh viễn
    public bool NeedsRenewal { get; set; }             // Có cần học lại không
    public int? RenewalAfterYears { get; set; }        // Số năm phải học lại
}

// ─── Employee ────────────────────────────────────────────────
public class EmployeeListDto
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Gender { get; set; } = "";
    public string? DateOfBirth { get; set; }
    public string? JoinDate { get; set; }
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = "";
    public string Position { get; set; } = "";
    public bool IsActive { get; set; }
    public int TotalHours { get; set; }           // Tổng tiết thực tế
    public bool IsCompliant { get; set; }
    public int MissingHours { get; set; }
    public int CertWarnings { get; set; }         // Số chứng chỉ cần chú ý
    public int NoEvidenceCount { get; set; }      // Số hồ sơ chưa có minh chứng
}

public class EmployeeDetailDto : EmployeeListDto
{
    public List<TrainingRecordDto> Trainings { get; set; } = new();
}

public class CreateEmployeeDto
{
    [Required(ErrorMessage = "Mã nhân viên là bắt buộc")]
    [MaxLength(20, ErrorMessage = "Mã nhân viên không được quá 20 ký tự")]
    public string EmployeeCode { get; set; } = "";

    [Required(ErrorMessage = "Họ và tên là bắt buộc")]
    [MaxLength(100, ErrorMessage = "Họ và tên không được quá 100 ký tự")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Giới tính là bắt buộc")]
    public string Gender { get; set; } = "Nam";

    public string? DateOfBirth { get; set; }

    [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ")]
    public string? Email { get; set; }

    public string? Phone { get; set; }

    [Required(ErrorMessage = "Phòng ban là bắt buộc")]
    public int DepartmentId { get; set; }

    [Required(ErrorMessage = "Chức danh là bắt buộc")]
    public string Position { get; set; } = "";

    public string? JoinDate { get; set; }
}

// ─── Training ────────────────────────────────────────────────
public class TrainingRecordDto
{
    public int TrainingId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = "";
    public string EmployeeCode { get; set; } = "";
    public string DepartmentName { get; set; } = "";
    public int CourseId { get; set; }
    public string CourseName { get; set; } = "";
    public string Organizer { get; set; } = "";
    /// <summary>Số tiết quy định của khóa học</summary>
    public int TrainingHours { get; set; }
    /// <summary>Số tiết thực tế đã học (nhập tay)</summary>
    public int ActualHours { get; set; }
    public string IssueDate { get; set; } = "";        // Ngày cấp
    public string ExpiryDate { get; set; } = "";       // Ngày hết hạn
    public string? Notes { get; set; }
    /// <summary>Tên file minh chứng (nếu có)</summary>
    public string? CertificateFile { get; set; }
    /// <summary>URL xem file minh chứng</summary>
    public string? CertificateUrl { get; set; }
    /// <summary>Có file minh chứng chưa</summary>
    public bool HasEvidence { get; set; }
    public string StatusLabel { get; set; } = "";
    public string BadgeClass { get; set; } = "";
    public int DaysLeft { get; set; }
    // Phân loại chứng chỉ
    public bool IsLifetime { get; set; }               // Vĩnh viễn
    public bool NeedsRenewal { get; set; }             // Có cần học lại
    public int? RenewalAfterYears { get; set; }        // Số năm học lại
    public string CertTypeLabel { get; set; } = "";    // "Vĩnh viễn" | "Có thời hạn"
    public string CertTypeBadge { get; set; } = "";    // badge class
}

public class CreateTrainingDto
{
    [Required(ErrorMessage = "Nhân viên là bắt buộc")]
    public int EmployeeId { get; set; }

    public int CourseId { get; set; }

    [Required(ErrorMessage = "Tên khóa học là bắt buộc")]
    public string CourseName { get; set; } = "";

    [Required(ErrorMessage = "Đơn vị tổ chức là bắt buộc")]
    public string Organizer { get; set; } = "";

    [Required(ErrorMessage = "Số tiết quy định là bắt buộc")]
    [Range(1, 500, ErrorMessage = "Số tiết quy định phải từ 1 đến 500")]
    public int TrainingHours { get; set; }

    [Required(ErrorMessage = "Số tiết thực tế là bắt buộc")]
    [Range(0, 500, ErrorMessage = "Số tiết thực tế phải từ 0 đến 500")]
    public int ActualHours { get; set; }

    [Required(ErrorMessage = "Ngày cấp là bắt buộc")]
    public string IssueDate { get; set; } = "";

    [Required(ErrorMessage = "Ngày hết hạn là bắt buộc")]
    public string ExpiryDate { get; set; } = "";

    public string? Notes { get; set; }
}

// ─── Settings ────────────────────────────────────────────────
public class SystemSettingsDto
{
    public int ExpiryWarningDays { get; set; } = 60;
    public int UrgentWarningDays { get; set; } = 30;
    public int RequiredHours1Year { get; set; } = 24;
    public int RequiredHours2Years { get; set; } = 48;
    public int RequiredHours5Years { get; set; } = 120;
}

// ─── Department ──────────────────────────────────────────────
public class DepartmentDto
{
    public int DepartmentId { get; set; }
    public string DepartmentCode { get; set; } = "";
    public string DepartmentName { get; set; } = "";
    public int EmployeeCount { get; set; }
}

// ─── TrainingCourse ──────────────────────────────────────────
public class TrainingCourseDto
{
    public int CourseId { get; set; }
    public string CourseCode { get; set; } = "";
    public string CourseName { get; set; } = "";
    public string Organizer { get; set; } = "";
    public int DefaultHours { get; set; }
    public bool IsLifetime { get; set; }
    public int? RequiresRenewalAfterYears { get; set; }
}

public class CreateCourseDto
{
    public string CourseCode { get; set; } = "CUSTOM";
    public string CourseName { get; set; } = "";
    public string Organizer { get; set; } = "";
    public int DefaultHours { get; set; }
    public string? Description { get; set; }
    public bool IsLifetime { get; set; } = false;
    public int? RequiresRenewalAfterYears { get; set; }
}

// ─── Authentication DTOs ─────────────────────────────────────
public class LoginDto
{
    [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
    [MaxLength(50, ErrorMessage = "Tên đăng nhập không được quá 50 ký tự")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    public string Password { get; set; } = "";
}

public class LoginResponseDto
{
    public string Token { get; set; } = "";
    public int UserId { get; set; }
    public string Username { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Role { get; set; } = "";
    public int? EmployeeId { get; set; }
    public bool RequirePasswordChange { get; set; }
}

public class ChangePasswordDto
{
    [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "Mật khẩu cũ là bắt buộc")]
    public string CurrentPassword { get; set; } = "";

    [Required(ErrorMessage = "Mật khẩu mới là bắt buộc")]
    [MinLength(8, ErrorMessage = "Mật khẩu mới phải có ít nhất 8 ký tự")]
    public string NewPassword { get; set; } = "";

    [Required(ErrorMessage = "Xác nhận mật khẩu mới là bắt buộc")]
    [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
    public string ConfirmPassword { get; set; } = "";
}

// ─── Audit Log DTO ───────────────────────────────────────────
public class AuditLogDto
{
    public int AuditLogId { get; set; }
    public int? UserId { get; set; }
    public string Username { get; set; } = "";
    public string Action { get; set; } = "";
    public string Description { get; set; } = "";
    public string CreatedAt { get; set; } = "";
}

// ─── User Management DTOs ─────────────────────────────────────
public class UserListDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Role { get; set; } = "";
    public int? EmployeeId { get; set; }
    public string? EmployeeCode { get; set; }
    public string? EmployeeName { get; set; }
    public string? DepartmentName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateUserDto
{
    [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
    [MaxLength(50, ErrorMessage = "Tên đăng nhập không được quá 50 ký tự")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "Họ và tên là bắt buộc")]
    [MaxLength(100, ErrorMessage = "Họ và tên không được quá 100 ký tự")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [MinLength(8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Vai trò là bắt buộc")]
    public string Role { get; set; } = ""; // Admin, HR, Manager, Viewer, User

    public int? EmployeeId { get; set; }
}

public class UpdateUserDto
{
    [Required(ErrorMessage = "Họ và tên là bắt buộc")]
    [MaxLength(100, ErrorMessage = "Họ và tên không được quá 100 ký tự")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Vai trò là bắt buộc")]
    public string Role { get; set; } = ""; // Admin, HR, Manager, Viewer, User

    public int? EmployeeId { get; set; }
    public bool IsActive { get; set; }
}

public class AdminResetPasswordDto
{
    [Required(ErrorMessage = "Mật khẩu mới là bắt buộc")]
    [MinLength(8, ErrorMessage = "Mật khẩu mới phải có ít nhất 8 ký tự")]
    public string NewPassword { get; set; } = "";
}
