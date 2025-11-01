using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BiSoyle.Product.Service.Migrations
{
    /// <inheritdoc />
    public partial class AddDevices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_products_UrunAdi",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_categories_KategoriAdi",
                table: "categories");

            migrationBuilder.AddColumn<int>(
                name: "KategoriId",
                table: "products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "categories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "devices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    CihazAdi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CihazTipi = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Marka = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BaglantiTipi = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Durum = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_devices", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_products_TenantId",
                table: "products",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_products_TenantId_UrunAdi",
                table: "products",
                columns: new[] { "TenantId", "UrunAdi" });

            migrationBuilder.CreateIndex(
                name: "IX_categories_TenantId",
                table: "categories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_categories_TenantId_KategoriAdi",
                table: "categories",
                columns: new[] { "TenantId", "KategoriAdi" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_devices_TenantId",
                table: "devices",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_devices_TenantId_CihazAdi",
                table: "devices",
                columns: new[] { "TenantId", "CihazAdi" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "devices");

            migrationBuilder.DropIndex(
                name: "IX_products_TenantId",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_products_TenantId_UrunAdi",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_categories_TenantId",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "IX_categories_TenantId_KategoriAdi",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "KategoriId",
                table: "products");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "products");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "categories");

            migrationBuilder.CreateIndex(
                name: "IX_products_UrunAdi",
                table: "products",
                column: "UrunAdi");

            migrationBuilder.CreateIndex(
                name: "IX_categories_KategoriAdi",
                table: "categories",
                column: "KategoriAdi",
                unique: true);
        }
    }
}
