using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CityFix.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkerMunicipality : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Specialization",
                table: "Workers",
                newName: "Municipality");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Municipality",
                table: "Workers",
                newName: "Specialization");
        }
    }
}
