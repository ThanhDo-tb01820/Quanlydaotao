using Microsoft.EntityFrameworkCore;
using CmeTracker.Api.Data;
using CmeTracker.Api.Models;
using CmeTracker.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ─── Services ────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        // Giữ nguyên tên camelCase cho JSON response
        opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// IHttpContextAccessor (dùng trong CMELogicService để build URL file)
builder.Services.AddHttpContextAccessor();

// EF Core + SQL Server LocalDB
builder.Services.AddDbContext<CmeTrackerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
           .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

// Đăng ký CME Logic Service (Scoped = mỗi request)
builder.Services.AddScoped<CMELogicService>();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title       = "CME Smart Tracker API",
        Version     = "v1",
        Description = "API quản lý đào tạo liên tục (CME) — Bệnh viện Hoàn Mỹ Đồng Nai",
    });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Nhập 'Bearer [token]' (ví dụ: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...)"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// CORS — Cho phép frontend (file HTML) gọi API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "HoanMyDongNaiCmeTrackerSecretKey2026!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "CmeTrackerApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "CmeTrackerClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var db = context.HttpContext.RequestServices.GetRequiredService<CmeTrackerDbContext>();
            var username = context.Principal?.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                context.Fail("Unauthorized");
                return;
            }
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username);
            if (user == null || !user.IsActive)
            {
                context.Fail("Tài khoản đã bị khóa hoặc không tồn tại.");
            }
        }
    };
});

var app = builder.Build();

app.UseMiddleware<CmeTracker.Api.Middleware.GlobalExceptionMiddleware>();

// ─── Auto-migrate database & seed users programmatically ──────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CmeTrackerDbContext>();
    db.Database.Migrate();

    var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();
    
    var usersToUpdate = new[] { "admin", "hr", "manager", "viewer" };
    foreach(var uname in usersToUpdate)
    {
        var u = db.Users.FirstOrDefault(x => x.Username == uname);
        if (u != null)
        {
            u.PasswordHash = hasher.HashPassword(u, uname);
        }
        else
        {
            var role = uname == "admin" ? "Admin" : (uname == "hr" ? "HR" : (uname == "manager" ? "Manager" : "Viewer"));
            var fullName = uname == "admin" ? "Hệ thống Admin" : (uname == "hr" ? "Ngân Thị" : (uname == "manager" ? "Trần Văn Quản Lý" : "Nguyễn Văn Xem"));
            var newUser = new User { Username = uname, FullName = fullName, Role = role, IsActive = true, CreatedAt = DateTime.Now };
            newUser.PasswordHash = hasher.HashPassword(newUser, uname);
            db.Users.Add(newUser);
        }
    }
    db.SaveChanges();
}

// ─── Tạo thư mục uploads nếu chưa có ────────────────────────
var uploadsPath = Path.Combine(app.Environment.WebRootPath ?? 
    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads");
Directory.CreateDirectory(uploadsPath);

// ─── Static files (phục vụ ảnh & PDF upload) ──────────────────
app.UseStaticFiles();

// Phục vụ frontend từ thư mục hiện tại (nơi chứa index.html, app.js, style.css, data.js)
app.UseDefaultFiles(new DefaultFilesOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        app.Environment.ContentRootPath),
    RequestPath = ""
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        app.Environment.ContentRootPath),
    RequestPath = ""
});

// ─── Middleware ───────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CME Tracker API v1");
        c.RoutePrefix = "swagger";  // Truy cập: http://localhost:5000/swagger
    });
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
