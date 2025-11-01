using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BiSoyle.User.Service.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVoiceSamplesSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_Email",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_Username",
                table: "users");

            migrationBuilder.AddColumn<int>(
                name: "CurrentVoiceSamples",
                table: "users",
                type: "integer",
                nullable: true,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxVoiceSamples",
                table: "users",
                type: "integer",
                nullable: true,
                defaultValue: 6);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "users",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "voice_samples",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Duration = table.Column<int>(type: "integer", nullable: false),
                    IsProcessed = table.Column<bool>(type: "boolean", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_voice_samples", x => x.Id);
                    table.ForeignKey(
                        name: "FK_voice_samples_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_TenantId_Email",
                table: "users",
                columns: new[] { "TenantId", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_users_TenantId_Username",
                table: "users",
                columns: new[] { "TenantId", "Username" });

            migrationBuilder.CreateIndex(
                name: "IX_voice_samples_UserId",
                table: "voice_samples",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "voice_samples");

            migrationBuilder.DropIndex(
                name: "IX_users_TenantId_Email",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_TenantId_Username",
                table: "users");

            migrationBuilder.DropColumn(
                name: "CurrentVoiceSamples",
                table: "users");

            migrationBuilder.DropColumn(
                name: "MaxVoiceSamples",
                table: "users");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_Username",
                table: "users",
                column: "Username",
                unique: true);
        }
    }
}
