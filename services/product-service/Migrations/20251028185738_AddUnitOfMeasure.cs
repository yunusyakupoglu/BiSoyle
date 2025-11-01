using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BiSoyle.Product.Service.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitOfMeasure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "unit_of_measures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BirimAdi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Kisaltma = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unit_of_measures", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_unit_of_measures_BirimAdi",
                table: "unit_of_measures",
                column: "BirimAdi",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_unit_of_measures_Kisaltma",
                table: "unit_of_measures",
                column: "Kisaltma",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "unit_of_measures");
        }
    }
}
