using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRequiredCostalesToWeightDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContpaqiComercialFolio",
                table: "WeightEntries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Costales",
                table: "WeightDetails",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContpaqiComercialFolio",
                table: "WeightEntries");

            migrationBuilder.DropColumn(
                name: "Costales",
                table: "WeightDetails");
        }
    }
}
