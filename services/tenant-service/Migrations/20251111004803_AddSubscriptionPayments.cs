using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BiSoyle.Tenant.Service.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "subscription_payments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    PlanId = table.Column<int>(type: "integer", nullable: false),
                    SubscriptionId = table.Column<int>(type: "integer", nullable: true),
                    Tutar = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    KomisyonTutari = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    ParaBirimi = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "TRY"),
                    TaksitSayisi = table.Column<int>(type: "integer", nullable: false),
                    KartSahibi = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    KartSon4 = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    ReferansKodu = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Durum = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    BankaAdi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Mesaj = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    OnayTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Kullanildi = table.Column<bool>(type: "boolean", nullable: false),
                    KullanimTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UcDSecure = table.Column<bool>(type: "boolean", nullable: false),
                    IslemNo = table.Column<string>(type: "text", nullable: true),
                    ProvizyonKodu = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_subscription_payments_subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payments_ReferansKodu",
                table: "subscription_payments",
                column: "ReferansKodu",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payments_SubscriptionId",
                table: "subscription_payments",
                column: "SubscriptionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_payments_TenantId_PlanId_Kullanildi",
                table: "subscription_payments",
                columns: new[] { "TenantId", "PlanId", "Kullanildi" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "subscription_payments");
        }
    }
}
