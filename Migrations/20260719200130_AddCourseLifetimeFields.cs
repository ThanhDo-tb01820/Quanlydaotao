using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CmeTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseLifetimeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "EmployeeTrainings",
                keyColumn: "TrainingId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "EmployeeTrainings",
                keyColumn: "TrainingId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "EmployeeTrainings",
                keyColumn: "TrainingId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "EmployeeTrainings",
                keyColumn: "TrainingId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "EmployeeTrainings",
                keyColumn: "TrainingId",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "EmployeeTrainings",
                keyColumn: "TrainingId",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "EmployeeTrainings",
                keyColumn: "TrainingId",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "EmployeeTrainings",
                keyColumn: "TrainingId",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "EmployeeTrainings",
                keyColumn: "TrainingId",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "EmployeeTrainings",
                keyColumn: "TrainingId",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "EmployeeTrainings",
                keyColumn: "TrainingId",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "EmployeeTrainings",
                keyColumn: "TrainingId",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "EmployeeTrainings",
                keyColumn: "TrainingId",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "EmployeeTrainings",
                keyColumn: "TrainingId",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "EmployeeTrainings",
                keyColumn: "TrainingId",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "EmployeeTrainings",
                keyColumn: "TrainingId",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "EmployeeTrainings",
                keyColumn: "TrainingId",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "EmployeeTrainings",
                keyColumn: "TrainingId",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "EmployeeId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "EmployeeId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "EmployeeId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "EmployeeId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "EmployeeId",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "EmployeeId",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "EmployeeId",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "EmployeeId",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "EmployeeId",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "EmployeeId",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "TrainingCourses",
                keyColumn: "CourseId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "TrainingCourses",
                keyColumn: "CourseId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "TrainingCourses",
                keyColumn: "CourseId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "TrainingCourses",
                keyColumn: "CourseId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "TrainingCourses",
                keyColumn: "CourseId",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "TrainingCourses",
                keyColumn: "CourseId",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "TrainingCourses",
                keyColumn: "CourseId",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "TrainingCourses",
                keyColumn: "CourseId",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "TrainingCourses",
                keyColumn: "CourseId",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "TrainingCourses",
                keyColumn: "CourseId",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "DepartmentId",
                keyValue: 9);

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Users",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "IsLifetime",
                table: "TrainingCourses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RequiresRenewalAfterYears",
                table: "TrainingCourses",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "IssueDate",
                table: "EmployeeTrainings",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "ExpiryDate",
                table: "EmployeeTrainings",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Username",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsLifetime",
                table: "TrainingCourses");

            migrationBuilder.DropColumn(
                name: "RequiresRenewalAfterYears",
                table: "TrainingCourses");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "IssueDate",
                table: "EmployeeTrainings",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1),
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "ExpiryDate",
                table: "EmployeeTrainings",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1),
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

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
                table: "Users",
                columns: new[] { "UserId", "CreatedAt", "EmployeeId", "FullName", "IsActive", "PasswordHash", "Role", "Username" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 6, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Hệ thống Admin", true, "AQAAAAIAAYagAAAAEAsPedAj4aoWYEaYTcQv1oXbXdyldgqfpEs5RSRKg+4gC/T8z3IJkpNmVMnTO1vfVQ==", "Admin", "admin" },
                    { 2, new DateTime(2026, 6, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Ngân Thị", true, "AQAAAAIAAYagAAAAEAASPkgrHWGulQ5hmf1XpMwRSyRvX8Cif2YptGSLtPowI0FNmqx1wiTjolaF8C24kg==", "HR", "hr" },
                    { 3, new DateTime(2026, 6, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Trần Văn Quản Lý", true, "AQAAAAIAAYagAAAAEIdW1mKN3kbQkQG0z4ALK8hMdqURDPJyTxoakTfqmaw0jYR3MLuVlXWYdY/dE0PBZA==", "Manager", "manager" },
                    { 4, new DateTime(2026, 6, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Nguyễn Văn Xem", true, "AQAAAAIAAYagAAAAEARBDgxamUgpBRchcSOm4pvj7iNsrqq/CST3xpJXbiTqf0/ZFP0UBQ8dGo/BfyyMRw==", "Viewer", "viewer" }
                });

            migrationBuilder.InsertData(
                table: "Employees",
                columns: new[] { "EmployeeId", "DateOfBirth", "DeletedAt", "DepartmentId", "Email", "EmployeeCode", "FullName", "Gender", "IsActive", "IsDeleted", "JoinDate", "Phone", "Position" },
                values: new object[,]
                {
                    { 1, new DateOnly(1985, 3, 12), null, 1, null, "NV001", "Nguyễn Văn An", "Nam", true, false, new DateOnly(2010, 8, 1), null, "Bác sĩ" },
                    { 2, new DateOnly(1990, 7, 22), null, 2, null, "NV002", "Trần Thị Bình", "Nữ", true, false, new DateOnly(2015, 1, 15), null, "Điều dưỡng" },
                    { 3, new DateOnly(1978, 11, 5), null, 5, null, "NV003", "Lê Hoàng Cường", "Nam", true, false, new DateOnly(2005, 6, 1), null, "Bác sĩ" },
                    { 4, new DateOnly(1992, 4, 18), null, 3, null, "NV004", "Phạm Thị Dung", "Nữ", true, false, new DateOnly(2018, 3, 10), null, "Hộ sinh" },
                    { 5, new DateOnly(1988, 9, 30), null, 6, null, "NV005", "Hoàng Minh Đức", "Nam", true, false, new DateOnly(2013, 5, 20), null, "Bác sĩ" },
                    { 6, new DateOnly(1995, 2, 14), null, 4, null, "NV006", "Ngô Thị Hoa", "Nữ", true, false, new DateOnly(2020, 7, 1), null, "Điều dưỡng" },
                    { 7, new DateOnly(1982, 6, 25), null, 7, null, "NV007", "Vũ Quốc Hùng", "Nam", true, false, new DateOnly(2008, 9, 15), null, "Kỹ thuật viên" },
                    { 8, new DateOnly(1993, 12, 8), null, 8, null, "NV008", "Đặng Thị Lan", "Nữ", true, false, new DateOnly(2017, 4, 22), null, "Kỹ thuật viên" },
                    { 9, new DateOnly(1975, 8, 16), null, 1, null, "NV009", "Bùi Văn Minh", "Nam", true, false, new DateOnly(2000, 1, 10), null, "Bác sĩ trưởng" },
                    { 10, new DateOnly(1991, 5, 3), null, 9, null, "NV010", "Đinh Thị Nga", "Nữ", true, false, new DateOnly(2016, 8, 30), null, "Dược sĩ" }
                });

            migrationBuilder.InsertData(
                table: "EmployeeTrainings",
                columns: new[] { "TrainingId", "ActualHours", "CertificateFile", "CourseId", "EmployeeId", "ExpiryDate", "IssueDate", "Notes", "TrainingHours" },
                values: new object[,]
                {
                    { 1, 0, null, 1, 1, new DateOnly(2026, 7, 5), new DateOnly(2025, 6, 15), null, 24 },
                    { 2, 0, null, 2, 1, new DateOnly(2026, 12, 30), new DateOnly(2025, 12, 17), null, 16 },
                    { 3, 0, null, 2, 2, new DateOnly(2026, 5, 1), new DateOnly(2023, 12, 28), null, 16 },
                    { 4, 0, null, 4, 2, new DateOnly(2027, 1, 1), new DateOnly(2025, 11, 28), null, 8 },
                    { 5, 0, null, 3, 3, new DateOnly(2027, 2, 10), new DateOnly(2026, 1, 16), null, 24 },
                    { 6, 0, null, 9, 3, new DateOnly(2027, 6, 30), new DateOnly(2026, 3, 7), null, 24 },
                    { 7, 0, null, 5, 4, new DateOnly(2027, 5, 11), new DateOnly(2026, 3, 17), null, 8 },
                    { 8, 0, null, 8, 5, new DateOnly(2026, 6, 5), new DateOnly(2024, 5, 11), null, 24 },
                    { 9, 0, null, 9, 5, new DateOnly(2026, 11, 12), new DateOnly(2026, 1, 6), null, 16 },
                    { 10, 0, null, 4, 6, new DateOnly(2026, 7, 30), new DateOnly(2025, 7, 30), null, 8 },
                    { 11, 0, null, 6, 7, new DateOnly(2027, 2, 28), new DateOnly(2025, 11, 28), null, 24 },
                    { 12, 0, null, 2, 7, new DateOnly(2027, 8, 19), new DateOnly(2026, 2, 15), null, 24 },
                    { 13, 0, null, 7, 8, new DateOnly(2026, 8, 9), new DateOnly(2025, 8, 19), null, 24 },
                    { 14, 0, null, 2, 8, new DateOnly(2027, 7, 19), new DateOnly(2026, 1, 16), null, 16 },
                    { 15, 0, null, 3, 9, new DateOnly(2027, 11, 6), new DateOnly(2026, 1, 6), null, 48 },
                    { 16, 0, null, 10, 10, new DateOnly(2027, 6, 10), new DateOnly(2025, 9, 9), null, 24 },
                    { 17, 0, null, 2, 10, new DateOnly(2027, 8, 29), new DateOnly(2026, 2, 5), null, 16 },
                    { 18, 0, null, 4, 10, new DateOnly(2027, 11, 27), new DateOnly(2026, 3, 17), null, 8 }
                });
        }
    }
}
