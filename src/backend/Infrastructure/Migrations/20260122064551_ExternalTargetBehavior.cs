using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExternalTargetBehavior : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExternalTargetBehaviorFK",
                table: "WeightEntries",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ExternalTargetBehavior",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TargetSerie = table.Column<string>(type: "text", nullable: true),
                    TargetName = table.Column<string>(type: "text", nullable: true),
                    TargetConcept = table.Column<string>(type: "text", nullable: true),
                    TargetDomain = table.Column<string>(type: "text", nullable: true),
                    TargetAlmacen = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalTargetBehavior", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeightEntries_ExternalTargetBehaviorFK",
                table: "WeightEntries",
                column: "ExternalTargetBehaviorFK");

            migrationBuilder.AddForeignKey(
                name: "FK_WeightEntries_ExternalTargetBehavior_ExternalTargetBehavior~",
                table: "WeightEntries",
                column: "ExternalTargetBehaviorFK",
                principalTable: "ExternalTargetBehavior",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WeightEntries_ExternalTargetBehavior_ExternalTargetBehavior~",
                table: "WeightEntries");

            migrationBuilder.DropTable(
                name: "ExternalTargetBehavior");

            migrationBuilder.DropIndex(
                name: "IX_WeightEntries_ExternalTargetBehaviorFK",
                table: "WeightEntries");

            migrationBuilder.DropColumn(
                name: "ExternalTargetBehaviorFK",
                table: "WeightEntries");
        }
    }
}
