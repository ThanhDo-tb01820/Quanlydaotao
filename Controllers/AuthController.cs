using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CmeTracker.Api.Data;
using CmeTracker.Api.Models;
using CmeTracker.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CmeTracker.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly CmeTrackerDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthController> _logger;

    public AuthController(CmeTrackerDbContext db, IConfiguration config, ILogger<AuthController> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
        if (user == null || !user.IsActive)
        {
            _db.AuditLogs.Add(new AuditLog
            {
                Username = dto.Username,
                Action = "Login Failed",
                Description = $"Failed login attempt for username: {dto.Username} (User not found or inactive)",
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();
            return BadRequest(new { message = "Tài khoản không tồn tại hoặc đã bị khóa!" });
        }

        var hasher = new PasswordHasher<User>();
        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            _db.AuditLogs.Add(new AuditLog
            {
                UserId = user.UserId,
                Username = user.Username,
                Action = "Login Failed",
                Description = $"Failed login attempt for username: {dto.Username} (Incorrect password)",
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();
            return BadRequest(new { message = "Mật khẩu không chính xác!" });
        }

        var token = GenerateJwtToken(user);

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = user.UserId,
            Username = user.Username,
            Action = "Login Success",
            Description = $"User {user.FullName} logged in successfully.",
            CreatedAt = DateTime.Now
        });
        await _db.SaveChangesAsync();

        return Ok(new LoginResponseDto
        {
            Token = token,
            UserId = user.UserId,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role,
            EmployeeId = user.EmployeeId
        });
    }

    [HttpPost("change-password")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var currentUser = User.Identity?.Name;
        if (currentUser != dto.Username)
        {
            return Forbid();
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
        if (user == null || !user.IsActive)
        {
            return NotFound(new { message = "Không tìm thấy người dùng!" });
        }

        var hasher = new PasswordHasher<User>();
        var verifyOld = hasher.VerifyHashedPassword(user, user.PasswordHash, dto.CurrentPassword);
        if (verifyOld == PasswordVerificationResult.Failed)
        {
            return BadRequest(new { message = "Mật khẩu cũ không chính xác!" });
        }

        var verifyNewIsSame = hasher.VerifyHashedPassword(user, user.PasswordHash, dto.NewPassword);
        if (verifyNewIsSame == PasswordVerificationResult.Success)
        {
            return BadRequest(new { message = "Mật khẩu mới không được trùng với mật khẩu cũ!" });
        }

        user.PasswordHash = hasher.HashPassword(user, dto.NewPassword);
        
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = user.UserId,
            Username = user.Username,
            Action = "Change Password",
            Description = $"User {user.FullName} changed their password successfully.",
            CreatedAt = DateTime.Now
        });
        await _db.SaveChangesAsync();

        return Ok(new { message = "Đổi mật khẩu thành công!" });
    }

    private string GenerateJwtToken(User user)
    {
        var jwtKey = _config["Jwt:Key"] ?? "HoanMyDongNaiCmeTrackerSecretKey2026!";
        var jwtIssuer = _config["Jwt:Issuer"] ?? "CmeTrackerApi";
        var jwtAudience = _config["Jwt:Audience"] ?? "CmeTrackerClient";

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(jwtKey);
        
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("FullName", user.FullName),
            new Claim("EmployeeId", user.EmployeeId?.ToString() ?? "")
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(8),
            Issuer = jwtIssuer,
            Audience = jwtAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
