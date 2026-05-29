using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CityFix.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddIsBlockedToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChangedByWorkerName",
                table: "ReportStatusHistories");

            migrationBuilder.AddColumn<bool>(
                name: "IsBlocked",
                table: "Workers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsBlocked",
                table: "Customers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsBlocked",
                table: "Admins",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsBlocked",
                table: "Workers");

            migrationBuilder.DropColumn(
                name: "IsBlocked",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "IsBlocked",
                table: "Admins");

            migrationBuilder.AddColumn<string>(
                name: "ChangedByWorkerName",
                table: "ReportStatusHistories",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
