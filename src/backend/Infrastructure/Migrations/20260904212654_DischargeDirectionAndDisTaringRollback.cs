using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DischargeDirectionAndDisTaringRollback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequiresDisTaring",
                table: "WeightDetails");

            migrationBuilder.AddColumn<bool>(
                name: "IsDischarge",
                table: "WeightEntries",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDischarge",
                table: "WeightEntries");

            migrationBuilder.AddColumn<bool>(
                name: "RequiresDisTaring",
                table: "WeightDetails",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
