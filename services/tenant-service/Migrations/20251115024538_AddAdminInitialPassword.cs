using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BiSoyle.Tenant.Service.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminInitialPassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminInitialPassword",
                table: "tenants",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminInitialPassword",
                table: "tenants");
        }
    }
}
