using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractStudentService.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomRegistrationAssignmentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedAt",
                table: "RoomRegistrations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignmentMode",
                table: "RoomRegistrations",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AssignmentNote",
                table: "RoomRegistrations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignedAt",
                table: "RoomRegistrations");

            migrationBuilder.DropColumn(
                name: "AssignmentMode",
                table: "RoomRegistrations");

            migrationBuilder.DropColumn(
                name: "AssignmentNote",
                table: "RoomRegistrations");
        }
    }
}
