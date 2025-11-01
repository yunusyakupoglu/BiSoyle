using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BiSoyle.Transaction.Service.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "transactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IslemKodu = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IslemTipi = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "SATIS"),
                    ToplamTutar = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    OdemeTipi = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ReceiptId = table.Column<int>(type: "integer", nullable: true),
                    Aciklama = table.Column<string>(type: "text", nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "transaction_items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TransactionId = table.Column<int>(type: "integer", nullable: false),
                    UrunId = table.Column<int>(type: "integer", nullable: false),
                    UrunAdi = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Miktar = table.Column<int>(type: "integer", nullable: false),
                    BirimFiyat = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transaction_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_transaction_items_transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_transaction_items_TransactionId",
                table: "transaction_items",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_IslemKodu",
                table: "transactions",
                column: "IslemKodu",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transactions_IslemTipi",
                table: "transactions",
                column: "IslemTipi");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "transaction_items");

            migrationBuilder.DropTable(
                name: "transactions");
        }
    }
}
