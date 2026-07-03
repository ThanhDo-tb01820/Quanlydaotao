using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CmeTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CMERequirements",
                columns: table => new
                {
                    RequirementId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PeriodYear = table.Column<int>(type: "int", nullable: false),
                    RequiredHours = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CMERequirements", x => x.RequirementId);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    DepartmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DepartmentName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.DepartmentId);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    SettingKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SettingValue = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.SettingKey);
                });

            migrationBuilder.CreateTable(
                name: "TrainingCourses",
                columns: table => new
                {
                    CourseId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CourseName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Organizer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefaultHours = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingCourses", x => x.CourseId);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    EmployeeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    Position = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JoinDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.EmployeeId);
                    table.ForeignKey(
                        name: "FK_Employees_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeTrainings",
                columns: table => new
                {
                    TrainingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    TrainingHours = table.Column<int>(type: "int", nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CertificateFile = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeTrainings", x => x.TrainingId);
                    table.ForeignKey(
                        name: "FK_EmployeeTrainings_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeTrainings_TrainingCourses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "TrainingCourses",
                        principalColumn: "CourseId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    NotificationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    NotificationType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.NotificationId);
                    table.ForeignKey(
                        name: "FK_Notifications_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "CMERequirements",
                columns: new[] { "RequirementId", "PeriodYear", "RequiredHours" },
                values: new object[,]
                {
                    { 1, 1, 24 },
                    { 2, 2, 48 },
                    { 3, 5, 120 }
                });

            migrationBuilder.InsertData(
                table: "Departments",
                columns: new[] { "DepartmentId", "DepartmentCode", "DepartmentName" },
                values: new object[,]
                {
                    { 1, "KHNT", "Khoa Ngoại Tổng hợp" },
                    { 2, "KHITT", "Khoa Nội Tổng hợp" },
                    { 3, "KHSAN", "Khoa Sản" },
                    { 4, "KHNHI", "Khoa Nhi" },
                    { 5, "KHCC", "Khoa Cấp cứu" },
                    { 6, "GMHS", "Khoa Gây mê Hồi sức" },
                    { 7, "KHXN", "Khoa Xét nghiệm" },
                    { 8, "CDHA", "Khoa Chẩn đoán hình ảnh" },
                    { 9, "DUOC", "Khoa Dược" },
                    { 10, "PHNS", "Phòng Nhân sự" },
                    { 11, "PHKT", "Phòng Kế toán" },
                    { 12, "PHHC", "Phòng Hành chính" }
                });

            migrationBuilder.InsertData(
                table: "SystemSettings",
                columns: new[] { "SettingKey", "SettingValue" },
                values: new object[,]
                {
                    { "ExpiryWarningDays", "60" },
                    { "RequiredHours1Year", "24" },
                    { "RequiredHours2Years", "48" },
                    { "RequiredHours5Years", "120" },
                    { "UrgentWarningDays", "30" }
                });

            migrationBuilder.InsertData(
                table: "TrainingCourses",
                columns: new[] { "CourseId", "CourseCode", "CourseName", "DefaultHours", "Description", "Organizer" },
                values: new object[,]
                {
                    { 1, "PALS", "Cấp cứu Nhi khoa nâng cao (PALS)", 24, null, "BV Nhi Đồng 1" },
                    { 2, "KSNK", "Kiểm soát nhiễm khuẩn cơ bản", 16, null, "Sở Y tế Đồng Nai" },
                    { 3, "ACLS", "Hỗ trợ sự sống tim mạch nâng cao (ACLS)", 24, null, "Hội Tim mạch VN" },
                    { 4, "BLS", "Hồi sức tích cực cơ bản (BLS)", 8, null, "Hội Tim mạch VN" },
                    { 5, "ALSO", "Hỗ trợ sự sống sản khoa (ALSO)", 32, null, "BV Từ Dũ" },
                    { 6, "HLATM", "Huyết học lâm sàng", 24, null, "Viện Huyết học" },
                    { 7, "SAUCB", "Kỹ thuật siêu âm cơ bản", 24, null, "Hội CĐHA VN" },
                    { 8, "GMNS", "Gây mê nhi khoa", 24, null, "BV Nhi Đồng 2" },
                    { 9, "ATPT", "An toàn phẫu thuật WHO", 16, null, "WHO / BV Từ Dũ" },
                    { 10, "DUOCLAM", "Dược lâm sàng nâng cao", 24, null, "Hội Dược học VN" }
                });

            migrationBuilder.InsertData(
                table: "Employees",
                columns: new[] { "EmployeeId", "DateOfBirth", "DepartmentId", "Email", "EmployeeCode", "FullName", "Gender", "IsActive", "JoinDate", "Phone", "Position" },
                values: new object[,]
                {
                    { 1, new DateOnly(1985, 3, 12), 1, null, "NV001", "Nguyễn Văn An", "Nam", true, new DateOnly(2010, 8, 1), null, "Bác sĩ" },
                    { 2, new DateOnly(1990, 7, 22), 2, null, "NV002", "Trần Thị Bình", "Nữ", true, new DateOnly(2015, 1, 15), null, "Điều dưỡng" },
                    { 3, new DateOnly(1978, 11, 5), 5, null, "NV003", "Lê Hoàng Cường", "Nam", true, new DateOnly(2005, 6, 1), null, "Bác sĩ" },
                    { 4, new DateOnly(1992, 4, 18), 3, null, "NV004", "Phạm Thị Dung", "Nữ", true, new DateOnly(2018, 3, 10), null, "Hộ sinh" },
                    { 5, new DateOnly(1988, 9, 30), 6, null, "NV005", "Hoàng Minh Đức", "Nam", true, new DateOnly(2013, 5, 20), null, "Bác sĩ" },
                    { 6, new DateOnly(1995, 2, 14), 4, null, "NV006", "Ngô Thị Hoa", "Nữ", true, new DateOnly(2020, 7, 1), null, "Điều dưỡng" },
                    { 7, new DateOnly(1982, 6, 25), 7, null, "NV007", "Vũ Quốc Hùng", "Nam", true, new DateOnly(2008, 9, 15), null, "Kỹ thuật viên" },
                    { 8, new DateOnly(1993, 12, 8), 8, null, "NV008", "Đặng Thị Lan", "Nữ", true, new DateOnly(2017, 4, 22), null, "Kỹ thuật viên" },
                    { 9, new DateOnly(1975, 8, 16), 1, null, "NV009", "Bùi Văn Minh", "Nam", true, new DateOnly(2000, 1, 10), null, "Bác sĩ trưởng" },
                    { 10, new DateOnly(1991, 5, 3), 9, null, "NV010", "Đinh Thị Nga", "Nữ", true, new DateOnly(2016, 8, 30), null, "Dược sĩ" }
                });

            migrationBuilder.InsertData(
                table: "EmployeeTrainings",
                columns: new[] { "TrainingId", "CertificateFile", "CourseId", "EmployeeId", "ExpiryDate", "IssueDate", "Notes", "TrainingHours" },
                values: new object[,]
                {
                    { 1, null, 1, 1, new DateOnly(2026, 7, 5), new DateOnly(2025, 6, 15), null, 24 },
                    { 2, null, 2, 1, new DateOnly(2026, 12, 30), new DateOnly(2025, 12, 17), null, 16 },
                    { 3, null, 2, 2, new DateOnly(2026, 5, 1), new DateOnly(2023, 12, 28), null, 16 },
                    { 4, null, 4, 2, new DateOnly(2027, 1, 1), new DateOnly(2025, 11, 28), null, 8 },
                    { 5, null, 3, 3, new DateOnly(2027, 2, 10), new DateOnly(2026, 1, 16), null, 24 },
                    { 6, null, 9, 3, new DateOnly(2027, 6, 30), new DateOnly(2026, 3, 7), null, 24 },
                    { 7, null, 5, 4, new DateOnly(2027, 5, 11), new DateOnly(2026, 3, 17), null, 8 },
                    { 8, null, 8, 5, new DateOnly(2026, 6, 5), new DateOnly(2024, 5, 11), null, 24 },
                    { 9, null, 9, 5, new DateOnly(2026, 11, 12), new DateOnly(2026, 1, 6), null, 16 },
                    { 10, null, 4, 6, new DateOnly(2026, 7, 30), new DateOnly(2025, 7, 30), null, 8 },
                    { 11, null, 6, 7, new DateOnly(2027, 2, 28), new DateOnly(2025, 11, 28), null, 24 },
                    { 12, null, 2, 7, new DateOnly(2027, 8, 19), new DateOnly(2026, 2, 15), null, 24 },
                    { 13, null, 7, 8, new DateOnly(2026, 8, 9), new DateOnly(2025, 8, 19), null, 24 },
                    { 14, null, 2, 8, new DateOnly(2027, 7, 19), new DateOnly(2026, 1, 16), null, 16 },
                    { 15, null, 3, 9, new DateOnly(2027, 11, 6), new DateOnly(2026, 1, 6), null, 48 },
                    { 16, null, 10, 10, new DateOnly(2027, 6, 10), new DateOnly(2025, 9, 9), null, 24 },
                    { 17, null, 2, 10, new DateOnly(2027, 8, 29), new DateOnly(2026, 2, 5), null, 16 },
                    { 18, null, 4, 10, new DateOnly(2027, 11, 27), new DateOnly(2026, 3, 17), null, 8 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_DepartmentId",
                table: "Employees",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTrainings_CourseId",
                table: "EmployeeTrainings",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTrainings_EmployeeId",
                table: "EmployeeTrainings",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_EmployeeId",
                table: "Notifications",
                column: "EmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CMERequirements");

            migrationBuilder.DropTable(
                name: "EmployeeTrainings");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "TrainingCourses");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "Departments");
        }
    }
}
