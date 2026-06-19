using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractStudentService.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomRegistrationRejectionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "RoomRegistrations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "RoomRegistrations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "RoomRegistrations");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "RoomRegistrations");
        }
    }
}
