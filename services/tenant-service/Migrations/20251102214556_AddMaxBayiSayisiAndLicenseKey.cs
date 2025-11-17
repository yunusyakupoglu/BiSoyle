using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BiSoyle.Tenant.Service.Migrations
{
    /// <inheritdoc />
    public partial class AddMaxBayiSayisiAndLicenseKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LicenseKey",
                table: "tenants",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LicenseKeyGirisTarihi",
                table: "tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxBayiSayisi",
                table: "subscription_plans",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_tenants_LicenseKey",
                table: "tenants",
                column: "LicenseKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tenants_LicenseKey",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "LicenseKey",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "LicenseKeyGirisTarihi",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "MaxBayiSayisi",
                table: "subscription_plans");
        }
    }
}
