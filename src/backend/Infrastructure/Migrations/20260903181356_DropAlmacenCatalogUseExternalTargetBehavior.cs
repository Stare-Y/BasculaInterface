using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropAlmacenCatalogUseExternalTargetBehavior : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WeightDetails_Almacenes_FK_AlmacenId",
                table: "WeightDetails");

            migrationBuilder.DropTable(
                name: "Almacenes");

            migrationBuilder.DropIndex(
                name: "IX_WeightDetails_FK_AlmacenId",
                table: "WeightDetails");

            migrationBuilder.DropColumn(
                name: "FK_AlmacenId",
                table: "WeightDetails");

            migrationBuilder.AddColumn<string>(
                name: "AlmacenName",
                table: "ExternalTargetBehaviors",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlmacenName",
                table: "ExternalTargetBehaviors");

            migrationBuilder.AddColumn<int>(
                name: "FK_AlmacenId",
                table: "WeightDetails",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Almacenes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Hidden = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Almacenes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeightDetails_FK_AlmacenId",
                table: "WeightDetails",
                column: "FK_AlmacenId");

            migrationBuilder.AddForeignKey(
                name: "FK_WeightDetails_Almacenes_FK_AlmacenId",
                table: "WeightDetails",
                column: "FK_AlmacenId",
                principalTable: "Almacenes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
