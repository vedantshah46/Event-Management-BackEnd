using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "EventOrganizationStatus",
                schema: "dbo",
                columns: table => new
                {
                    EventOrganizationStatusId = table.Column<int>(type: "int", nullable: false),
                    EventOrganizationStatus = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventOrganizationStatus", x => x.EventOrganizationStatusId);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationRoles",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PlatformRole = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.CheckConstraint("CK_Users_PlatformRole", "[PlatformRole] IN ('Admin', 'Organizer', 'CrewMember')");
                });

            migrationBuilder.CreateTable(
                name: "EventOrganizations",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    EventOrganizationStatusId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventOrganizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventOrganizations_EventOrganizationStatus_EventOrganizationStatusId",
                        column: x => x.EventOrganizationStatusId,
                        principalSchema: "dbo",
                        principalTable: "EventOrganizationStatus",
                        principalColumn: "EventOrganizationStatusId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationRolePermissions",
                schema: "dbo",
                columns: table => new
                {
                    OrganizationRoleId = table.Column<int>(type: "int", nullable: false),
                    PermissionCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationRolePermissions", x => new { x.OrganizationRoleId, x.PermissionCode });
                    table.ForeignKey(
                        name: "FK_OrganizationRolePermissions_OrganizationRoles_OrganizationRoleId",
                        column: x => x.OrganizationRoleId,
                        principalSchema: "dbo",
                        principalTable: "OrganizationRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserClaims",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserClaims_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLogins",
                schema: "dbo",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_UserLogins_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationMemberships",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventOrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationRoleId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    JoinedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationMemberships_EventOrganizations_EventOrganizationId",
                        column: x => x.EventOrganizationId,
                        principalSchema: "dbo",
                        principalTable: "EventOrganizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationMemberships_OrganizationRoles_OrganizationRoleId",
                        column: x => x.OrganizationRoleId,
                        principalSchema: "dbo",
                        principalTable: "OrganizationRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationMemberships_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationPaymentDetails",
                schema: "dbo",
                columns: table => new
                {
                    EventOrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpiId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationPaymentDetails", x => x.EventOrganizationId);
                    table.ForeignKey(
                        name: "FK_OrganizationPaymentDetails_EventOrganizations_EventOrganizationId",
                        column: x => x.EventOrganizationId,
                        principalSchema: "dbo",
                        principalTable: "EventOrganizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationPaymentDetails_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationStatusHistory",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventOrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousEventOrganizationStatusId = table.Column<int>(type: "int", nullable: true),
                    NewEventOrganizationStatusId = table.Column<int>(type: "int", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationStatusHistory_EventOrganizationStatus_NewEventOrganizationStatusId",
                        column: x => x.NewEventOrganizationStatusId,
                        principalSchema: "dbo",
                        principalTable: "EventOrganizationStatus",
                        principalColumn: "EventOrganizationStatusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationStatusHistory_EventOrganizationStatus_PreviousEventOrganizationStatusId",
                        column: x => x.PreviousEventOrganizationStatusId,
                        principalSchema: "dbo",
                        principalTable: "EventOrganizationStatus",
                        principalColumn: "EventOrganizationStatusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationStatusHistory_EventOrganizations_EventOrganizationId",
                        column: x => x.EventOrganizationId,
                        principalSchema: "dbo",
                        principalTable: "EventOrganizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationStatusHistory_Users_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "EventOrganizationStatus",
                columns: new[] { "EventOrganizationStatusId", "EventOrganizationStatus", "IsDeleted" },
                values: new object[,]
                {
                    { 1, "Pending approval", false },
                    { 2, "Approved", false },
                    { 3, "Rejected", false },
                    { 4, "Suspended", false }
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "OrganizationRoles",
                columns: new[] { "Id", "Code", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, "OWNER", true, "Owner" },
                    { 2, "STAFFING_MANAGER", true, "Staffing Manager" },
                    { 3, "ATTENDANCE_MANAGER", true, "Attendance Manager" }
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "OrganizationRolePermissions",
                columns: new[] { "OrganizationRoleId", "PermissionCode" },
                values: new object[,]
                {
                    { 1, "attendance.approve" },
                    { 1, "attendance.record" },
                    { 1, "events.manage" },
                    { 1, "finance.read" },
                    { 1, "organization.payment-details.manage" },
                    { 1, "organization.profile.read" },
                    { 1, "organization.staff.manage" },
                    { 1, "staffing.manage" },
                    { 2, "events.manage" },
                    { 2, "organization.profile.read" },
                    { 2, "staffing.manage" },
                    { 3, "attendance.approve" },
                    { 3, "attendance.record" },
                    { 3, "organization.profile.read" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventOrganizations_EventOrganizationStatusId_CreatedAtUtc",
                schema: "dbo",
                table: "EventOrganizations",
                columns: new[] { "EventOrganizationStatusId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EventOrganizationStatus_EventOrganizationStatus",
                schema: "dbo",
                table: "EventOrganizationStatus",
                column: "EventOrganizationStatus",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMemberships_EventOrganizationId_IsActive",
                schema: "dbo",
                table: "OrganizationMemberships",
                columns: new[] { "EventOrganizationId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMemberships_OrganizationRoleId",
                schema: "dbo",
                table: "OrganizationMemberships",
                column: "OrganizationRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMemberships_UserId",
                schema: "dbo",
                table: "OrganizationMemberships",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationPaymentDetails_UpdatedByUserId",
                schema: "dbo",
                table: "OrganizationPaymentDetails",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationRoles_Code",
                schema: "dbo",
                table: "OrganizationRoles",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationStatusHistory_ChangedByUserId",
                schema: "dbo",
                table: "OrganizationStatusHistory",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationStatusHistory_EventOrganizationId_ChangedAtUtc",
                schema: "dbo",
                table: "OrganizationStatusHistory",
                columns: new[] { "EventOrganizationId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationStatusHistory_NewEventOrganizationStatusId",
                schema: "dbo",
                table: "OrganizationStatusHistory",
                column: "NewEventOrganizationStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationStatusHistory_PreviousEventOrganizationStatusId",
                schema: "dbo",
                table: "OrganizationStatusHistory",
                column: "PreviousEventOrganizationStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_UserClaims_UserId",
                schema: "dbo",
                table: "UserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogins_UserId",
                schema: "dbo",
                table: "UserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "dbo",
                table: "Users",
                column: "NormalizedEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "dbo",
                table: "Users",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrganizationMemberships",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "OrganizationPaymentDetails",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "OrganizationRolePermissions",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "OrganizationStatusHistory",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "UserClaims",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "UserLogins",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "OrganizationRoles",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "EventOrganizations",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "EventOrganizationStatus",
                schema: "dbo");
        }
    }
}
