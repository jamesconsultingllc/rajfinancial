#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace RajFinancial.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUniqueEmailIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_Email",
                table: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_TenantId_Email",
                table: "UserProfiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_Email",
                table: "UserProfiles",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_TenantId_Email",
                table: "UserProfiles",
                columns: new[] { "TenantId", "Email" },
                unique: true);
        }
    }
}
