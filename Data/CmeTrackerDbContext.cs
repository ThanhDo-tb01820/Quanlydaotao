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
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();
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

        // Bỏ comment Seed Data ở dưới nếu dùng cho CSDL trống ban đầu.
        // Hiện tại đã import dữ liệu thật từ file Excel nên không cần seed data này nữa.
        /*
        // Phòng ban
        modelBuilder.Entity<Department>().HasData( ... );
        // Danh mục khóa học
        modelBuilder.Entity<TrainingCourse>().HasData( ... );
        // Nhân viên
        modelBuilder.Entity<Employee>().HasData( ... );
        // Chứng chỉ đào tạo
        modelBuilder.Entity<EmployeeTraining>().HasData( ... );
        */

        // Seed users removed from EF Migration to prevent dynamic password hash model issues
    }
}
