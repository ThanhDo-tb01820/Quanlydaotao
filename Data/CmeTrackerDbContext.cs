using Microsoft.EntityFrameworkCore;
using CmeTracker.Api.Models;

namespace CmeTracker.Api.Data;

public class CmeTrackerDbContext : DbContext
{
    public CmeTrackerDbContext(DbContextOptions<CmeTrackerDbContext> options) : base(options) { }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<TrainingCourse> TrainingCourses => Set<TrainingCourse>();
    public DbSet<EmployeeTraining> EmployeeTrainings => Set<EmployeeTraining>();
    public DbSet<CMERequirement> CMERequirements => Set<CMERequirement>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<User> Users => Set<User>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // SystemSetting: khóa chính là SettingKey (string)
        modelBuilder.Entity<SystemSetting>()
            .HasKey(s => s.SettingKey);

        // Explicit primary keys for all entities
        modelBuilder.Entity<CMERequirement>().HasKey(r => r.RequirementId);
        modelBuilder.Entity<User>().HasKey(u => u.UserId);
        modelBuilder.Entity<AuditLog>().HasKey(a => a.AuditLogId);

        // Global Query Filter for Soft Delete
        modelBuilder.Entity<Employee>().HasQueryFilter(e => !e.IsDeleted);

        // User -> Employee relationship
        modelBuilder.Entity<User>()
            .HasOne(u => u.Employee)
            .WithMany()
            .HasForeignKey(u => u.EmployeeId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Department>().HasKey(d => d.DepartmentId);
        modelBuilder.Entity<Employee>().HasKey(e => e.EmployeeId);
        modelBuilder.Entity<TrainingCourse>().HasKey(c => c.CourseId);
        modelBuilder.Entity<EmployeeTraining>().HasKey(t => t.TrainingId);
        modelBuilder.Entity<Notification>().HasKey(n => n.NotificationId);

        // Employee → Department (nhiều NV thuộc 1 phòng ban)
        modelBuilder.Entity<Employee>()
            .HasOne(e => e.Department)
            .WithMany(d => d.Employees)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // EmployeeTraining → Employee
        modelBuilder.Entity<EmployeeTraining>()
            .HasOne(t => t.Employee)
            .WithMany(e => e.Trainings)
            .HasForeignKey(t => t.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        // EmployeeTraining → TrainingCourse
        modelBuilder.Entity<EmployeeTraining>()
            .HasOne(t => t.Course)
            .WithMany(c => c.EmployeeTrainings)
            .HasForeignKey(t => t.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        // Notification → Employee
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.Employee)
            .WithMany()
            .HasForeignKey(n => n.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        // ─── SEED DATA ────────────────────────────────────────────

        // Quy định CME
        modelBuilder.Entity<CMERequirement>().HasData(
            new CMERequirement { RequirementId = 1, PeriodYear = 1, RequiredHours = 24 },
            new CMERequirement { RequirementId = 2, PeriodYear = 2, RequiredHours = 48 },
            new CMERequirement { RequirementId = 3, PeriodYear = 5, RequiredHours = 120 }
        );

        // Cài đặt hệ thống mặc định
        modelBuilder.Entity<SystemSetting>().HasData(
            new SystemSetting { SettingKey = "ExpiryWarningDays",   SettingValue = "60" },
            new SystemSetting { SettingKey = "UrgentWarningDays",   SettingValue = "30" },
            new SystemSetting { SettingKey = "RequiredHours1Year",  SettingValue = "24" },
            new SystemSetting { SettingKey = "RequiredHours2Years", SettingValue = "48" },
            new SystemSetting { SettingKey = "RequiredHours5Years", SettingValue = "120" }
        );

        // Phòng ban
        modelBuilder.Entity<Department>().HasData(
            new Department { DepartmentId = 1,  DepartmentCode = "KHNT",  DepartmentName = "Khoa Ngoại Tổng hợp" },
            new Department { DepartmentId = 2,  DepartmentCode = "KHITT", DepartmentName = "Khoa Nội Tổng hợp" },
            new Department { DepartmentId = 3,  DepartmentCode = "KHSAN", DepartmentName = "Khoa Sản" },
            new Department { DepartmentId = 4,  DepartmentCode = "KHNHI", DepartmentName = "Khoa Nhi" },
            new Department { DepartmentId = 5,  DepartmentCode = "KHCC",  DepartmentName = "Khoa Cấp cứu" },
            new Department { DepartmentId = 6,  DepartmentCode = "GMHS",  DepartmentName = "Khoa Gây mê Hồi sức" },
            new Department { DepartmentId = 7,  DepartmentCode = "KHXN",  DepartmentName = "Khoa Xét nghiệm" },
            new Department { DepartmentId = 8,  DepartmentCode = "CDHA",  DepartmentName = "Khoa Chẩn đoán hình ảnh" },
            new Department { DepartmentId = 9,  DepartmentCode = "DUOC",  DepartmentName = "Khoa Dược" },
            new Department { DepartmentId = 10, DepartmentCode = "PHNS",  DepartmentName = "Phòng Nhân sự" },
            new Department { DepartmentId = 11, DepartmentCode = "PHKT",  DepartmentName = "Phòng Kế toán" },
            new Department { DepartmentId = 12, DepartmentCode = "PHHC",  DepartmentName = "Phòng Hành chính" }
        );

        // Danh mục khóa học
        modelBuilder.Entity<TrainingCourse>().HasData(
            new TrainingCourse { CourseId = 1,  CourseCode = "PALS",    CourseName = "Cấp cứu Nhi khoa nâng cao (PALS)",       Organizer = "BV Nhi Đồng 1",        DefaultHours = 24 },
            new TrainingCourse { CourseId = 2,  CourseCode = "KSNK",    CourseName = "Kiểm soát nhiễm khuẩn cơ bản",            Organizer = "Sở Y tế Đồng Nai",     DefaultHours = 16 },
            new TrainingCourse { CourseId = 3,  CourseCode = "ACLS",    CourseName = "Hỗ trợ sự sống tim mạch nâng cao (ACLS)", Organizer = "Hội Tim mạch VN",      DefaultHours = 24 },
            new TrainingCourse { CourseId = 4,  CourseCode = "BLS",     CourseName = "Hồi sức tích cực cơ bản (BLS)",           Organizer = "Hội Tim mạch VN",      DefaultHours = 8  },
            new TrainingCourse { CourseId = 5,  CourseCode = "ALSO",    CourseName = "Hỗ trợ sự sống sản khoa (ALSO)",          Organizer = "BV Từ Dũ",             DefaultHours = 32 },
            new TrainingCourse { CourseId = 6,  CourseCode = "HLATM",   CourseName = "Huyết học lâm sàng",                      Organizer = "Viện Huyết học",        DefaultHours = 24 },
            new TrainingCourse { CourseId = 7,  CourseCode = "SAUCB",   CourseName = "Kỹ thuật siêu âm cơ bản",                Organizer = "Hội CĐHA VN",          DefaultHours = 24 },
            new TrainingCourse { CourseId = 8,  CourseCode = "GMNS",    CourseName = "Gây mê nhi khoa",                         Organizer = "BV Nhi Đồng 2",        DefaultHours = 24 },
            new TrainingCourse { CourseId = 9,  CourseCode = "ATPT",    CourseName = "An toàn phẫu thuật WHO",                  Organizer = "WHO / BV Từ Dũ",       DefaultHours = 16 },
            new TrainingCourse { CourseId = 10, CourseCode = "DUOCLAM", CourseName = "Dược lâm sàng nâng cao",                  Organizer = "Hội Dược học VN",      DefaultHours = 24 }
        );

        // Nhân viên (10 người mẫu — đủ các trường hợp cảnh báo)
        // Lưu ý: Seed data phải dùng ngày cố định (không dùng DateTime.Today trong migration)
        // Ngày tham chiếu: 2026-06-15 — Cập nhật lại nếu cần
        modelBuilder.Entity<Employee>().HasData(
            new Employee { EmployeeId = 1,  EmployeeCode = "NV001", FullName = "Nguyễn Văn An",    Gender = "Nam", DateOfBirth = new DateOnly(1985, 3,  12), DepartmentId = 1, Position = "Bác sĩ",         JoinDate = new DateOnly(2010, 8, 1),  IsActive = true },
            new Employee { EmployeeId = 2,  EmployeeCode = "NV002", FullName = "Trần Thị Bình",    Gender = "Nữ",  DateOfBirth = new DateOnly(1990, 7,  22), DepartmentId = 2, Position = "Điều dưỡng",     JoinDate = new DateOnly(2015, 1, 15), IsActive = true },
            new Employee { EmployeeId = 3,  EmployeeCode = "NV003", FullName = "Lê Hoàng Cường",   Gender = "Nam", DateOfBirth = new DateOnly(1978, 11, 5),  DepartmentId = 5, Position = "Bác sĩ",         JoinDate = new DateOnly(2005, 6, 1),  IsActive = true },
            new Employee { EmployeeId = 4,  EmployeeCode = "NV004", FullName = "Phạm Thị Dung",    Gender = "Nữ",  DateOfBirth = new DateOnly(1992, 4,  18), DepartmentId = 3, Position = "Hộ sinh",        JoinDate = new DateOnly(2018, 3, 10), IsActive = true },
            new Employee { EmployeeId = 5,  EmployeeCode = "NV005", FullName = "Hoàng Minh Đức",   Gender = "Nam", DateOfBirth = new DateOnly(1988, 9,  30), DepartmentId = 6, Position = "Bác sĩ",         JoinDate = new DateOnly(2013, 5, 20), IsActive = true },
            new Employee { EmployeeId = 6,  EmployeeCode = "NV006", FullName = "Ngô Thị Hoa",      Gender = "Nữ",  DateOfBirth = new DateOnly(1995, 2,  14), DepartmentId = 4, Position = "Điều dưỡng",     JoinDate = new DateOnly(2020, 7, 1),  IsActive = true },
            new Employee { EmployeeId = 7,  EmployeeCode = "NV007", FullName = "Vũ Quốc Hùng",     Gender = "Nam", DateOfBirth = new DateOnly(1982, 6,  25), DepartmentId = 7, Position = "Kỹ thuật viên",  JoinDate = new DateOnly(2008, 9, 15), IsActive = true },
            new Employee { EmployeeId = 8,  EmployeeCode = "NV008", FullName = "Đặng Thị Lan",     Gender = "Nữ",  DateOfBirth = new DateOnly(1993, 12, 8),  DepartmentId = 8, Position = "Kỹ thuật viên",  JoinDate = new DateOnly(2017, 4, 22), IsActive = true },
            new Employee { EmployeeId = 9,  EmployeeCode = "NV009", FullName = "Bùi Văn Minh",     Gender = "Nam", DateOfBirth = new DateOnly(1975, 8,  16), DepartmentId = 1, Position = "Bác sĩ trưởng",  JoinDate = new DateOnly(2000, 1, 10), IsActive = true },
            new Employee { EmployeeId = 10, EmployeeCode = "NV010", FullName = "Đinh Thị Nga",     Gender = "Nữ",  DateOfBirth = new DateOnly(1991, 5,  3),  DepartmentId = 9, Position = "Dược sĩ",        JoinDate = new DateOnly(2016, 8, 30), IsActive = true }
        );

        // Chứng chỉ đào tạo — ngày cố định tính từ 2026-06-15
        modelBuilder.Entity<EmployeeTraining>().HasData(
            // NV001 - Sắp hết hạn 20 ngày (cam)
            new EmployeeTraining { TrainingId = 1,  EmployeeId = 1, CourseId = 1, TrainingHours = 24, IssueDate = new DateOnly(2025, 6, 15),  ExpiryDate = new DateOnly(2026, 7, 5)   },
            new EmployeeTraining { TrainingId = 2,  EmployeeId = 1, CourseId = 2, TrainingHours = 16, IssueDate = new DateOnly(2025, 12, 17), ExpiryDate = new DateOnly(2026, 12, 30) },
            // NV002 - Đã hết hạn 45 ngày (đỏ)
            new EmployeeTraining { TrainingId = 3,  EmployeeId = 2, CourseId = 2, TrainingHours = 16, IssueDate = new DateOnly(2023, 12, 28), ExpiryDate = new DateOnly(2026, 5, 1)   },
            new EmployeeTraining { TrainingId = 4,  EmployeeId = 2, CourseId = 4, TrainingHours = 8,  IssueDate = new DateOnly(2025, 11, 28), ExpiryDate = new DateOnly(2027, 1, 1)   },
            // NV003 - Đủ tiết, còn hiệu lực
            new EmployeeTraining { TrainingId = 5,  EmployeeId = 3, CourseId = 3, TrainingHours = 24, IssueDate = new DateOnly(2026, 1, 16),  ExpiryDate = new DateOnly(2027, 2, 10)  },
            new EmployeeTraining { TrainingId = 6,  EmployeeId = 3, CourseId = 9, TrainingHours = 24, IssueDate = new DateOnly(2026, 3, 7),   ExpiryDate = new DateOnly(2027, 6, 30)  },
            // NV004 - Thiếu tiết (8 tiết)
            new EmployeeTraining { TrainingId = 7,  EmployeeId = 4, CourseId = 5, TrainingHours = 8,  IssueDate = new DateOnly(2026, 3, 17),  ExpiryDate = new DateOnly(2027, 5, 11)  },
            // NV005 - Hết hạn 10 ngày trước (đỏ khẩn)
            new EmployeeTraining { TrainingId = 8,  EmployeeId = 5, CourseId = 8, TrainingHours = 24, IssueDate = new DateOnly(2024, 5, 11),  ExpiryDate = new DateOnly(2026, 6, 5)   },
            new EmployeeTraining { TrainingId = 9,  EmployeeId = 5, CourseId = 9, TrainingHours = 16, IssueDate = new DateOnly(2026, 1, 6),   ExpiryDate = new DateOnly(2026, 11, 12) },
            // NV006 - Sắp hết hạn 45 ngày (vàng) + thiếu tiết
            new EmployeeTraining { TrainingId = 10, EmployeeId = 6, CourseId = 4, TrainingHours = 8,  IssueDate = new DateOnly(2025, 7, 30),  ExpiryDate = new DateOnly(2026, 7, 30)  },
            // NV007 - Đủ tiết, còn hiệu lực
            new EmployeeTraining { TrainingId = 11, EmployeeId = 7, CourseId = 6, TrainingHours = 24, IssueDate = new DateOnly(2025, 11, 28), ExpiryDate = new DateOnly(2027, 2, 28)  },
            new EmployeeTraining { TrainingId = 12, EmployeeId = 7, CourseId = 2, TrainingHours = 24, IssueDate = new DateOnly(2026, 2, 15),  ExpiryDate = new DateOnly(2027, 8, 19)  },
            // NV008 - Sắp hết hạn 55 ngày (vàng)
            new EmployeeTraining { TrainingId = 13, EmployeeId = 8, CourseId = 7, TrainingHours = 24, IssueDate = new DateOnly(2025, 8, 19),  ExpiryDate = new DateOnly(2026, 8, 9)   },
            new EmployeeTraining { TrainingId = 14, EmployeeId = 8, CourseId = 2, TrainingHours = 16, IssueDate = new DateOnly(2026, 1, 16),  ExpiryDate = new DateOnly(2027, 7, 19)  },
            // NV009 - Đủ tiết, còn hiệu lực tốt
            new EmployeeTraining { TrainingId = 15, EmployeeId = 9, CourseId = 3, TrainingHours = 48, IssueDate = new DateOnly(2026, 1, 6),   ExpiryDate = new DateOnly(2027, 11, 6)  },
            // NV010 - Đủ tiết
            new EmployeeTraining { TrainingId = 16, EmployeeId = 10, CourseId = 10, TrainingHours = 24, IssueDate = new DateOnly(2025, 9, 9),  ExpiryDate = new DateOnly(2027, 6, 10)  },
            new EmployeeTraining { TrainingId = 17, EmployeeId = 10, CourseId = 2,  TrainingHours = 16, IssueDate = new DateOnly(2026, 2, 5),  ExpiryDate = new DateOnly(2027, 8, 29)  },
            new EmployeeTraining { TrainingId = 18, EmployeeId = 10, CourseId = 4,  TrainingHours = 8,  IssueDate = new DateOnly(2026, 3, 17), ExpiryDate = new DateOnly(2027, 11, 27) }
        );

        // Seed users removed from EF Migration to prevent dynamic password hash model issues
    }
}
