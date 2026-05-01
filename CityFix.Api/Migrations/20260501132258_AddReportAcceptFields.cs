using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CityFix.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddReportAcceptFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AcceptedAt",
                table: "Reports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignedWorkerEmail",
                table: "Reports",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptedAt",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "AssignedWorkerEmail",
                table: "Reports");
        }
    }
}
