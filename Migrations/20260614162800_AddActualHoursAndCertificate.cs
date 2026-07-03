using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CmeTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddActualHoursAndCertificate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActualHours",
                table: "EmployeeTrainings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "EmployeeTrainings",
                keyColumn: "TrainingId",
                keyValue: 1,
                column: "ActualHours",
                value: 0);

            migrationBuilder.UpdateData(
                table: "EmployeeTrainings",
                keyColumn: "TrainingId",
                keyValue: 2,
                column: "ActualHours",
                value: 0);

            migrationBuilder.UpdateData(
                table: "EmployeeTrainings",
                keyColumn: "TrainingId",
                keyValue: 3,
                column: "ActualHours",
                value: 0);

            migrationBuilder.UpdateData(
                table: "EmployeeTrainings",
                keyColumn: "TrainingId",
                keyValue: 4,
                column: "ActualHours",
                value: 0);

            migrationBuilder.UpdateData(
                table: "EmployeeTrainings",
                keyColumn: "TrainingId",
                keyValue: 5,
                column: "ActualHours",
                value: 0);

            migrationBuilder.UpdateData(
                table: "EmployeeTrainings",
                keyColumn: "TrainingId",
                keyValue: 6,
                column: "ActualHours",
                value: 0);

            migrationBuilder.UpdateData(
                table: "EmployeeTrainings",
                keyColumn: "TrainingId",
                keyValue: 7,
                column: "ActualHours",
                value: 0);

            migrationBuilder.UpdateData(
                table: "EmployeeTrainings",
                keyColumn: "TrainingId",
                keyValue: 8,
                column: "ActualHours",
                value: 0);

            migrationBuilder.UpdateData(
                table: "EmployeeTrainings",
                keyColumn: "TrainingId",
                keyValue: 9,
                column: "ActualHours",
                value: 0);

            migrationBuilder.UpdateData(
                table: "EmployeeTrainings",
                keyColumn: "TrainingId",
                keyValue: 10,
                column: "ActualHours",
                value: 0);

            migrationBuilder.UpdateData(
                table: "EmployeeTrainings",
                keyColumn: "TrainingId",
                keyValue: 11,
                column: "ActualHours",
                value: 0);

            migrationBuilder.UpdateData(
                table: "EmployeeTrainings",
                keyColumn: "TrainingId",
                keyValue: 12,
                column: "ActualHours",
                value: 0);

            migrationBuilder.UpdateData(
                table: "EmployeeTrainings",
                keyColumn: "TrainingId",
                keyValue: 13,
                column: "ActualHours",
                value: 0);

            migrationBuilder.UpdateData(
                table: "EmployeeTrainings",
                keyColumn: "TrainingId",
                keyValue: 14,
                column: "ActualHours",
                value: 0);

            migrationBuilder.UpdateData(
                table: "EmployeeTrainings",
                keyColumn: "TrainingId",
                keyValue: 15,
                column: "ActualHours",
                value: 0);

            migrationBuilder.UpdateData(
                table: "EmployeeTrainings",
                keyColumn: "TrainingId",
                keyValue: 16,
                column: "ActualHours",
                value: 0);

            migrationBuilder.UpdateData(
                table: "EmployeeTrainings",
                keyColumn: "TrainingId",
                keyValue: 17,
                column: "ActualHours",
                value: 0);

            migrationBuilder.UpdateData(
                table: "EmployeeTrainings",
                keyColumn: "TrainingId",
                keyValue: 18,
                column: "ActualHours",
                value: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualHours",
                table: "EmployeeTrainings");
        }
    }
}
