using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractStudentService.Migrations
{
    /// <inheritdoc />
    public partial class AddContractPdfSignatureFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SignedFileCreatedAt",
                table: "Contracts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedFileName",
                table: "Contracts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SignedFilePath",
                table: "Contracts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TemplateFileName",
                table: "Contracts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TemplateFilePath",
                table: "Contracts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "TemplateUploadedAt",
                table: "Contracts",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SignedFileCreatedAt",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "SignedFileName",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "SignedFilePath",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "TemplateFileName",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "TemplateFilePath",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "TemplateUploadedAt",
                table: "Contracts");
        }
    }
}
