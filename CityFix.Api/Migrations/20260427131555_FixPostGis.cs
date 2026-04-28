using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace CityFix.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixPostGis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Reports",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<Point>(
                name: "LocationPoint",
                table: "Reports",
                type: "geometry(Point,4326)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Reports",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateIndex(
                name: "IX_Reports_LocationPoint",
                table: "Reports",
                column: "LocationPoint")
                .Annotation("Npgsql:IndexMethod", "GIST");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reports_LocationPoint",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "LocationPoint",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Reports");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:postgis", ",,");
        }
    }
}
