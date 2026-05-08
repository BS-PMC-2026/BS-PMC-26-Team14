using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CityFix.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkerImageUploadToReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WorkerImageBase64",
                table: "Reports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkerImageNote",
                table: "Reports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WorkerImageUploadedAt",
                table: "Reports",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WorkerImageBase64",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "WorkerImageNote",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "WorkerImageUploadedAt",
                table: "Reports");
        }
    }
}
