using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IntermediateWeightColumnsAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "SecondaryTare",
                table: "WeightDetails",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WeightedBy",
                table: "WeightDetails",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SecondaryTare",
                table: "WeightDetails");

            migrationBuilder.DropColumn(
                name: "WeightedBy",
                table: "WeightDetails");
        }
    }
}
