using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedEventsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "dbo",
                table: "EventOrganizations");

            migrationBuilder.CreateTable(
                name: "EventStatus",
                schema: "dbo",
                columns: table => new
                {
                    EventStatusId = table.Column<int>(type: "int", nullable: false),
                    EventStatus = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventStatus", x => x.EventStatusId);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                schema: "dbo",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventOrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventStatusId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    VenueName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    StartsAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndsAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RequiredCrewMembers = table.Column<int>(type: "int", nullable: false),
                    HourlyPayRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AreBreaksPaid = table.Column<bool>(type: "bit", nullable: false),
                    ApplicationsCloseAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.EventId);
                    table.CheckConstraint("CK_Events_CrewAndPay", "[RequiredCrewMembers] > 0 AND [HourlyPayRate] > 0");
                    table.CheckConstraint("CK_Events_Dates", "[EndsAtUtc] > [StartsAtUtc] AND [ApplicationsCloseAtUtc] <= [StartsAtUtc]");
                    table.ForeignKey(
                        name: "FK_Events_EventOrganizations_EventOrganizationId",
                        column: x => x.EventOrganizationId,
                        principalSchema: "dbo",
                        principalTable: "EventOrganizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Events_EventStatus_EventStatusId",
                        column: x => x.EventStatusId,
                        principalSchema: "dbo",
                        principalTable: "EventStatus",
                        principalColumn: "EventStatusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Events_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Events_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "EventStatus",
                columns: new[] { "EventStatusId", "EventStatus", "IsDeleted" },
                values: new object[] { 1, "Draft", false });

            migrationBuilder.CreateIndex(
                name: "IX_Events_CreatedByUserId",
                schema: "dbo",
                table: "Events",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_EventOrganizationId_CreatedAtUtc_EventId",
                schema: "dbo",
                table: "Events",
                columns: new[] { "EventOrganizationId", "CreatedAtUtc", "EventId" });

            migrationBuilder.CreateIndex(
                name: "IX_Events_EventOrganizationId_EventStatusId",
                schema: "dbo",
                table: "Events",
                columns: new[] { "EventOrganizationId", "EventStatusId" });

            migrationBuilder.CreateIndex(
                name: "IX_Events_EventStatusId",
                schema: "dbo",
                table: "Events",
                column: "EventStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_UpdatedByUserId",
                schema: "dbo",
                table: "Events",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EventStatus_EventStatus",
                schema: "dbo",
                table: "EventStatus",
                column: "EventStatus",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Events",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "EventStatus",
                schema: "dbo");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "dbo",
                table: "EventOrganizations",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }
    }
}
