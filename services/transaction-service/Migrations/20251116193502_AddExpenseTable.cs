using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BiSoyle.Transaction.Service.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_transactions_IslemKodu",
                table: "transactions");

            migrationBuilder.CreateTable(
                name: "expenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    GiderAdi = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Tutar = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Kategori = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Aciklama = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_expenses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_expenses_OlusturmaTarihi",
                table: "expenses",
                column: "OlusturmaTarihi");

            migrationBuilder.CreateIndex(
                name: "IX_expenses_TenantId",
                table: "expenses",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "expenses");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_IslemKodu",
                table: "transactions",
                column: "IslemKodu");
        }
    }
}
