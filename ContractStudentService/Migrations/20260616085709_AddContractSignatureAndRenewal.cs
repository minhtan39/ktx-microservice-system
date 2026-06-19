using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractStudentService.Migrations
{
    /// <inheritdoc />
    public partial class AddContractSignatureAndRenewal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastRenewedAt",
                table: "Contracts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RenewalCount",
                table: "Contracts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RenewalNote",
                table: "Contracts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SignatureFullName",
                table: "Contracts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SignatureHash",
                table: "Contracts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SignatureIpAddress",
                table: "Contracts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SignatureStudentCode",
                table: "Contracts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "SignedAt",
                table: "Contracts",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastRenewedAt",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "RenewalCount",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "RenewalNote",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "SignatureFullName",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "SignatureHash",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "SignatureIpAddress",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "SignatureStudentCode",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "SignedAt",
                table: "Contracts");
        }
    }
}
