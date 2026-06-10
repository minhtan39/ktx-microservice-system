using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractStudentService.Migrations
{
    /// <inheritdoc />
    public partial class AddContractStudentRubricFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClassName",
                table: "Students",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FacultyName",
                table: "Students",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResidenceHistory",
                table: "Students",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PriorityNote",
                table: "RoomRegistrations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PriorityScore",
                table: "RoomRegistrations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PriorityType",
                table: "RoomRegistrations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Terms",
                table: "Contracts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClassName",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "FacultyName",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "ResidenceHistory",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "PriorityNote",
                table: "RoomRegistrations");

            migrationBuilder.DropColumn(
                name: "PriorityScore",
                table: "RoomRegistrations");

            migrationBuilder.DropColumn(
                name: "PriorityType",
                table: "RoomRegistrations");

            migrationBuilder.DropColumn(
                name: "Terms",
                table: "Contracts");
        }
    }
}
