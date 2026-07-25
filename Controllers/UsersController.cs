using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CmeTracker.Api.Data;
using CmeTracker.Api.DTOs;
using CmeTracker.Api.Models;

namespace CmeTracker.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly CmeTrackerDbContext _db;

    public UsersController(CmeTrackerDbContext db)
    {
        _db = db;
    }

    /// <summary>GET /api/v1/users — Danh sách tài khoản người dùng</summary>
    [HttpGet]
    public async Task<ActionResult<List<UserListDto>>> GetAll()
    {
        var users = await _db.Users
            .Include(u => u.Employee)
            .ThenInclude(e => e!.Department)
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new UserListDto
            {
                UserId = u.UserId,
                Username = u.Username,
                FullName = u.FullName,
                Role = u.Role,
                EmployeeId = u.EmployeeId,
                EmployeeCode = u.Employee != null ? u.Employee.EmployeeCode : null,
                EmployeeName = u.Employee != null ? u.Employee.FullName : null,
                DepartmentName = u.Employee != null && u.Employee.Department != null ? u.Employee.Department.DepartmentName : null,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    /// <summary>POST /api/v1/users — Thêm tài khoản người dùng mới</summary>
    [HttpPost]
    public async Task<ActionResult<UserListDto>> Create([FromBody] CreateUserDto dto)
    {
        var existing = await _db.Users.AnyAsync(u => u.Username == dto.Username);
        if (existing)
        {
            return BadRequest(new { message = "Tên đăng nhập đã tồn tại!" });
        }

        // Ràng buộc 1-1 giữa User và Employee
        if (dto.EmployeeId.HasValue)
        {
            var isLinked = await _db.Users.AnyAsync(u => u.EmployeeId == dto.EmployeeId);
            if (isLinked)
            {
                return BadRequest(new { message = "Nhân viên này đã được liên kết với một tài khoản khác!" });
            }
        }

        var user = new User
        {
            Username = dto.Username,
            FullName = dto.FullName,
            Role = dto.Role,
            EmployeeId = dto.EmployeeId,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        var hasher = new PasswordHasher<User>();
        user.PasswordHash = hasher.HashPassword(user, dto.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Ghi Audit Log
        _db.AuditLogs.Add(new AuditLog
        {
            Username = User.Identity?.Name ?? "Admin",
            Action = "Create User",
            Description = $"Admin created user: {user.Username} ({user.FullName}) - Role: {user.Role}",
            CreatedAt = DateTime.Now
        });
        await _db.SaveChangesAsync();

        return Ok(new UserListDto
        {
            UserId = user.UserId,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role,
            EmployeeId = user.EmployeeId,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        });
    }

    /// <summary>PUT /api/v1/users/{id} — Cập nhật thông tin tài khoản</summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto dto)
    {
        using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var currentUsername = User.Identity?.Name;
            
            // 1. Ngăn tự khóa tài khoản hoặc hạ quyền chính mình
            if (user.Username == currentUsername && (dto.Role != "Admin" || !dto.IsActive))
            {
                return BadRequest(new { message = "Bạn không thể tự hạ quyền hoặc tự khóa tài khoản của chính mình!" });
            }

            // 2. Ngăn hạ quyền hoặc khóa tài khoản Admin hoạt động cuối cùng
            if (user.Role == "Admin" && (dto.Role != "Admin" || !dto.IsActive))
            {
                var otherAdminsCount = await _db.Users.CountAsync(u => u.UserId != id && u.Role == "Admin" && u.IsActive);
                if (otherAdminsCount == 0)
                {
                    return BadRequest(new { message = "Không thể đổi vai trò hoặc khóa tài khoản Admin hoạt động cuối cùng của hệ thống!" });
                }
            }

            // 3. Ràng buộc 1-1 loại trừ chính User đang sửa
            if (dto.EmployeeId.HasValue)
            {
                var isLinked = await _db.Users.AnyAsync(u => u.EmployeeId == dto.EmployeeId && u.UserId != id);
                if (isLinked)
                {
                    return BadRequest(new { message = "Nhân viên này đã được liên kết với một tài khoản khác!" });
                }
            }

            var oldRole = user.Role;
            var oldStatus = user.IsActive;

            user.FullName = dto.FullName;
            user.Role = dto.Role;
            user.EmployeeId = dto.EmployeeId;
            user.IsActive = dto.IsActive;

            await _db.SaveChangesAsync();

            // Ghi Audit Log
            _db.AuditLogs.Add(new AuditLog
            {
                Username = currentUsername ?? "Admin",
                Action = "Update User",
                Description = $"Admin updated user {user.Username}. Role: {oldRole} -> {user.Role}, Active: {oldStatus} -> {user.IsActive}",
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();

            await transaction.CommitAsync();
            return NoContent();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return StatusCode(StatusCodes.Status409Conflict, new { message = "Hệ thống đang bận xử lý yêu cầu đồng thời. Vui lòng thử lại sau giây lát." });
        }
    }

    /// <summary>PUT /api/v1/users/{id}/reset-password — Đặt lại mật khẩu</summary>
    [HttpPut("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, [FromBody] AdminResetPasswordDto dto)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var hasher = new PasswordHasher<User>();
        user.PasswordHash = hasher.HashPassword(user, dto.NewPassword);
        await _db.SaveChangesAsync();

        // Ghi Audit Log
        _db.AuditLogs.Add(new AuditLog
        {
            Username = User.Identity?.Name ?? "Admin",
            Action = "Reset Password",
            Description = $"Admin reset password for user: {user.Username}",
            CreatedAt = DateTime.Now
        });
        await _db.SaveChangesAsync();

        return Ok(new { message = "Đặt lại mật khẩu thành công!" });
    }

    /// <summary>DELETE /api/v1/users/{id} — Xóa tài khoản người dùng</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var currentUsername = User.Identity?.Name;

            // 1. Ngăn tự xóa tài khoản của chính mình
            if (user.Username == currentUsername)
            {
                return BadRequest(new { message = "Bạn không thể tự xóa tài khoản của chính mình!" });
            }

            // 2. Ngăn xóa tài khoản Admin cuối cùng
            if (user.Role == "Admin")
            {
                var otherAdminsCount = await _db.Users.CountAsync(u => u.UserId != id && u.Role == "Admin" && u.IsActive);
                if (otherAdminsCount == 0)
                {
                    return BadRequest(new { message = "Không thể xóa tài khoản Admin hoạt động cuối cùng của hệ thống!" });
                }
            }

            var username = user.Username;
            _db.Users.Remove(user);
            await _db.SaveChangesAsync();

            // Ghi Audit Log
            _db.AuditLogs.Add(new AuditLog
            {
                Username = currentUsername ?? "Admin",
                Action = "Delete User",
                Description = $"Admin deleted user account: {username}",
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();

            await transaction.CommitAsync();
            return NoContent();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return StatusCode(StatusCodes.Status409Conflict, new { message = "Hệ thống đang bận xử lý yêu cầu đồng thời. Vui lòng thử lại sau giây lát." });
        }
    }

    /// <summary>POST /api/v1/users/auto-generate — Tạo tài khoản hàng loạt cho NV chưa có</summary>
    [HttpPost("auto-generate")]
    public async Task<IActionResult> AutoGenerateAccounts()
    {
        var currentUsername = User.Identity?.Name ?? "Admin";

        var unlinkedEmployees = await _db.Employees
            .Where(e => e.IsActive && !_db.Users.Any(u => u.EmployeeId == e.EmployeeId))
            .ToListAsync();

        if (unlinkedEmployees.Count == 0)
        {
            return Ok(new { message = "Tất cả nhân viên đang hoạt động đã có tài khoản!" });
        }

        var hasher = new PasswordHasher<User>();
        int count = 0;

        foreach (var emp in unlinkedEmployees)
        {
            if (await _db.Users.AnyAsync(u => u.Username == emp.EmployeeCode))
                continue;

            var newUser = new User
            {
                Username = emp.EmployeeCode,
                FullName = emp.FullName,
                Role = "User",
                EmployeeId = emp.EmployeeId,
                IsActive = true,
                RequirePasswordChange = true,
                CreatedAt = DateTime.Now
            };

            newUser.PasswordHash = hasher.HashPassword(newUser, "123456@Aa");
            _db.Users.Add(newUser);
            count++;
        }

        if (count > 0)
        {
            _db.AuditLogs.Add(new AuditLog
            {
                Username = currentUsername,
                Action = "Auto Generate Users",
                Description = $"Admin auto-generated {count} user accounts.",
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();
        }

        return Ok(new { message = $"Đã tạo thành công {count} tài khoản mới cho nhân viên!" });
    }
}
