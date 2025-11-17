using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BiSoyle.Tenant.Service.Migrations
{
    /// <inheritdoc />
    public partial class AddLicenseDeviceBinding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "licenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    LicenseKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LicenseJson = table.Column<string>(type: "text", nullable: false),
                    Signature = table.Column<string>(type: "text", nullable: false),
                    Package = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MaxInstallations = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    DeviceFingerprint = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ToleranceThreshold = table.Column<double>(type: "double precision", nullable: false, defaultValue: 1.0),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_licenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_licenses_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "license_activations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LicenseId = table.Column<int>(type: "integer", nullable: false),
                    DeviceFingerprint = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TpmPublicKey = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ActivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeviceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_license_activations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_license_activations_licenses_LicenseId",
                        column: x => x.LicenseId,
                        principalTable: "licenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rebind_requests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LicenseId = table.Column<int>(type: "integer", nullable: false),
                    OldDeviceFingerprint = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    NewDeviceFingerprint = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Pending"),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProcessedByUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rebind_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rebind_requests_licenses_LicenseId",
                        column: x => x.LicenseId,
                        principalTable: "licenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_license_activations_LicenseId_DeviceFingerprint",
                table: "license_activations",
                columns: new[] { "LicenseId", "DeviceFingerprint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_licenses_LicenseKey",
                table: "licenses",
                column: "LicenseKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_licenses_TenantId",
                table: "licenses",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_rebind_requests_LicenseId",
                table: "rebind_requests",
                column: "LicenseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "license_activations");

            migrationBuilder.DropTable(
                name: "rebind_requests");

            migrationBuilder.DropTable(
                name: "licenses");
        }
    }
}
