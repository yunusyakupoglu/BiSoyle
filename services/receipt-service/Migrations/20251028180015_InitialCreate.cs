using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BiSoyle.Receipt.Service.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "receipts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    IslemKodu = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ToplamTutar = table.Column<double>(type: "numeric(18,2)", nullable: false),
                    PdfPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_receipts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "receipt_items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReceiptId = table.Column<int>(type: "integer", nullable: false),
                    UrunId = table.Column<int>(type: "integer", nullable: false),
                    UrunAdi = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Miktar = table.Column<int>(type: "integer", nullable: false),
                    BirimFiyat = table.Column<double>(type: "numeric(18,2)", nullable: false),
                    OlcuBirimi = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Subtotal = table.Column<double>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_receipt_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_receipt_items_receipts_ReceiptId",
                        column: x => x.ReceiptId,
                        principalTable: "receipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_receipt_items_ReceiptId",
                table: "receipt_items",
                column: "ReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_receipts_TenantId",
                table: "receipts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_receipts_TenantId_IslemKodu",
                table: "receipts",
                columns: new[] { "TenantId", "IslemKodu" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "receipt_items");

            migrationBuilder.DropTable(
                name: "receipts");
        }
    }
}
