using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PedidosRemake : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProviderPurchases");

            migrationBuilder.AddColumn<int>(
                name: "FK_AlmacenId",
                table: "WeightDetails",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FK_PedidoLineId",
                table: "WeightDetails",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresDisTaring",
                table: "WeightDetails",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Almacenes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Hidden = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Almacenes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pedidos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProviderId = table.Column<int>(type: "integer", nullable: false),
                    ExpectedArrival = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pedidos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PedidoLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PedidoId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    RequiredAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    ManuallyClosed = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresDisTaring = table.Column<bool>(type: "boolean", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidoLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidoLines_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeightDetails_FK_AlmacenId",
                table: "WeightDetails",
                column: "FK_AlmacenId");

            migrationBuilder.CreateIndex(
                name: "IX_WeightDetails_FK_PedidoLineId",
                table: "WeightDetails",
                column: "FK_PedidoLineId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoLines_PedidoId",
                table: "PedidoLines",
                column: "PedidoId");

            migrationBuilder.AddForeignKey(
                name: "FK_WeightDetails_Almacenes_FK_AlmacenId",
                table: "WeightDetails",
                column: "FK_AlmacenId",
                principalTable: "Almacenes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_WeightDetails_PedidoLines_FK_PedidoLineId",
                table: "WeightDetails",
                column: "FK_PedidoLineId",
                principalTable: "PedidoLines",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WeightDetails_Almacenes_FK_AlmacenId",
                table: "WeightDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_WeightDetails_PedidoLines_FK_PedidoLineId",
                table: "WeightDetails");

            migrationBuilder.DropTable(
                name: "Almacenes");

            migrationBuilder.DropTable(
                name: "PedidoLines");

            migrationBuilder.DropTable(
                name: "Pedidos");

            migrationBuilder.DropIndex(
                name: "IX_WeightDetails_FK_AlmacenId",
                table: "WeightDetails");

            migrationBuilder.DropIndex(
                name: "IX_WeightDetails_FK_PedidoLineId",
                table: "WeightDetails");

            migrationBuilder.DropColumn(
                name: "FK_AlmacenId",
                table: "WeightDetails");

            migrationBuilder.DropColumn(
                name: "FK_PedidoLineId",
                table: "WeightDetails");

            migrationBuilder.DropColumn(
                name: "RequiresDisTaring",
                table: "WeightDetails");

            migrationBuilder.CreateTable(
                name: "ProviderPurchases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WeightEntryId = table.Column<int>(type: "integer", nullable: true),
                    Concluded = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpectedArrival = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Price = table.Column<decimal>(type: "numeric", nullable: true),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    ProviderId = table.Column<int>(type: "integer", nullable: false),
                    RealAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    RequiredAmount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderPurchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProviderPurchases_WeightEntries_WeightEntryId",
                        column: x => x.WeightEntryId,
                        principalTable: "WeightEntries",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderPurchases_WeightEntryId",
                table: "ProviderPurchases",
                column: "WeightEntryId");
        }
    }
}
