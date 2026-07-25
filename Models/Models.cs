namespace CmeTracker.Api.Models;

public class Department
{
    public int DepartmentId { get; set; }
    public string DepartmentCode { get; set; } = "";
    public string DepartmentName { get; set; } = "";

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}

public class Employee
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = "";   // Mã NV (NV001...)
    public string FullName { get; set; } = "";
    public string Gender { get; set; } = "";
    public DateOnly? DateOfBirth { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }

    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    public string Position { get; set; } = "";        // Chức danh
    public DateOnly? JoinDate { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    public ICollection<EmployeeTraining> Trainings { get; set; } = new List<EmployeeTraining>();
}

public class TrainingCourse
{
    public int CourseId { get; set; }
    public string CourseCode { get; set; } = "";
    public string CourseName { get; set; } = "";
    public string Organizer { get; set; } = "";
    public int DefaultHours { get; set; }
    public string? Description { get; set; }

    /// <summary>
    /// Chứng chỉ vĩnh viễn (true) = không cần cảnh báo, không cần đào tạo lại.
    /// Ví dụ: An toàn bức xạ, Chứng chỉ hành nghề
    /// </summary>
    public bool IsLifetime { get; set; } = false;

    /// <summary>
    /// Số năm phải học lại (null hoặc 0 = vĩnh viễn).
    /// Ví dụ: Phẫu thuật nội soi = 2, Cố xương khớp = 2
    /// </summary>
    public int? RequiresRenewalAfterYears { get; set; }

    public ICollection<EmployeeTraining> EmployeeTrainings { get; set; } = new List<EmployeeTraining>();
}

public class EmployeeTraining
{
    public int TrainingId { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public int CourseId { get; set; }
    public TrainingCourse Course { get; set; } = null!;

    /// <summary>Số tiết quy định của khóa học (do đơn vị tổ chức quy định)</summary>
    public int TrainingHours { get; set; }

    /// <summary>Số tiết thực tế nhân viên đã tham dự (nhập tay, có thể < TrainingHours)</summary>
    public int ActualHours { get; set; }

    public DateOnly? IssueDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }

    /// <summary>Đường dẫn file minh chứng (ảnh điểm danh, scan chứng chỉ, PDF)</summary>
    public string? CertificateFile { get; set; }
    public string? Notes { get; set; }
}

public class CMERequirement
{
    public int RequirementId { get; set; }
    public int PeriodYear { get; set; }       // 1, 2, 5
    public int RequiredHours { get; set; }    // 24, 48, 120
}

public class SystemSetting
{
    public string SettingKey { get; set; } = "";
    public string SettingValue { get; set; } = "";
}

public class Notification
{
    public int NotificationId { get; set; }
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public string NotificationType { get; set; } = ""; // Expired | ExpiringSoon | MissingHours
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class User
{
    public int UserId { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Role { get; set; } = ""; // Admin, HR, Manager, Viewer
    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public bool IsActive { get; set; } = true;
    public bool RequirePasswordChange { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class AuditLog
{
    public int AuditLogId { get; set; }
    public int? UserId { get; set; }
    public string Username { get; set; } = "";
    public string Action { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
