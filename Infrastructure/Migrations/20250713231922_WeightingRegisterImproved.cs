using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class WeightingRegisterImproved : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NetWeight",
                table: "WeightEntries",
                newName: "BruteWeight");

            migrationBuilder.AddColumn<double>(
                name: "Tare",
                table: "WeightDetails",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tare",
                table: "WeightDetails");

            migrationBuilder.RenameColumn(
                name: "BruteWeight",
                table: "WeightEntries",
                newName: "NetWeight");
        }
    }
}
