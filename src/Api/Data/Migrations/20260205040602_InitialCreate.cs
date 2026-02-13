using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RajFinancial.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsProfileComplete = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PreferencesJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataAccessGrants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GrantorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GranteeUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GranteeEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AccessType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Categories = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RelationshipLabel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InvitationToken = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    InvitationExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataAccessGrants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataAccessGrants_UserProfiles_GranteeUserId",
                        column: x => x.GranteeUserId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DataAccessGrants_UserProfiles_GrantorUserId",
                        column: x => x.GrantorUserId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DataAccessGrants_GranteeEmail",
                table: "DataAccessGrants",
                column: "GranteeEmail");

            migrationBuilder.CreateIndex(
                name: "IX_DataAccessGrants_GranteeUserId",
                table: "DataAccessGrants",
                column: "GranteeUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DataAccessGrants_GrantorUserId_GranteeUserId",
                table: "DataAccessGrants",
                columns: new[] { "GrantorUserId", "GranteeUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_DataAccessGrants_InvitationToken",
                table: "DataAccessGrants",
                column: "InvitationToken",
                unique: true,
                filter: "[InvitationToken] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DataAccessGrants_Status",
                table: "DataAccessGrants",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_Email",
                table: "UserProfiles",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_TenantId",
                table: "UserProfiles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_TenantId_Email",
                table: "UserProfiles",
                columns: new[] { "TenantId", "Email" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataAccessGrants");

            migrationBuilder.DropTable(
                name: "UserProfiles");
        }
    }
}
