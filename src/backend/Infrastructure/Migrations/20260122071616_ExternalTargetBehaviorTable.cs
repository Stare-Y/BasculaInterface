using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExternalTargetBehaviorTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WeightEntries_ExternalTargetBehavior_ExternalTargetBehavior~",
                table: "WeightEntries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExternalTargetBehavior",
                table: "ExternalTargetBehavior");

            migrationBuilder.RenameTable(
                name: "ExternalTargetBehavior",
                newName: "ExternalTargetBehaviors");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExternalTargetBehaviors",
                table: "ExternalTargetBehaviors",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WeightEntries_ExternalTargetBehaviors_ExternalTargetBehavio~",
                table: "WeightEntries",
                column: "ExternalTargetBehaviorFK",
                principalTable: "ExternalTargetBehaviors",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WeightEntries_ExternalTargetBehaviors_ExternalTargetBehavio~",
                table: "WeightEntries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExternalTargetBehaviors",
                table: "ExternalTargetBehaviors");

            migrationBuilder.RenameTable(
                name: "ExternalTargetBehaviors",
                newName: "ExternalTargetBehavior");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExternalTargetBehavior",
                table: "ExternalTargetBehavior",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WeightEntries_ExternalTargetBehavior_ExternalTargetBehavior~",
                table: "WeightEntries",
                column: "ExternalTargetBehaviorFK",
                principalTable: "ExternalTargetBehavior",
                principalColumn: "Id");
        }
    }
}
