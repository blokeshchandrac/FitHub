using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSmartEquipmentAndFaultReporting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ResolvedAt",
                table: "FaultReports",
                newName: "ResolutionDate");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "FaultReports",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Other");

            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "FaultReports",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSeriousSafetyConcern",
                table: "FaultReports",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MaintenanceNotes",
                table: "FaultReports",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EventType",
                table: "EquipmentStatusEvents",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "LegacyStatusChange");

            migrationBuilder.AddColumn<string>(
                name: "PreviousState",
                table: "EquipmentStatusEvents",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Available");

            migrationBuilder.Sql("UPDATE \"FaultReports\" SET \"Comment\" = \"Description\" WHERE \"Comment\" IS NULL;");
            migrationBuilder.Sql("UPDATE \"FaultReports\" SET \"Status\" = 'UnderReview' WHERE \"Status\" = 'InProgress';");
            migrationBuilder.Sql("UPDATE \"FaultReports\" SET \"Status\" = CASE WHEN \"ResolutionDate\" IS NULL THEN 'Rejected' ELSE 'Resolved' END WHERE \"Status\" = 'Closed';");
            migrationBuilder.Sql("UPDATE \"EquipmentStatusEvents\" SET \"PreviousState\" = \"State\";");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ClearanceGraceStartedAt",
                table: "Equipment",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                table: "Equipment",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PartnerUserId",
                table: "Equipment",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PresenceChangedAt",
                table: "Equipment",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PresenceDetected",
                table: "Equipment",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StateBeforeOffline",
                table: "Equipment",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EquipmentDevices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CredentialHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    LastHeartbeatAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsConnected = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentDevices_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MemberAccessCards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UidHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberAccessCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberAccessCards_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_OwnerUserId",
                table: "Equipment",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_PartnerUserId",
                table: "Equipment",
                column: "PartnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentDevices_DeviceCode",
                table: "EquipmentDevices",
                column: "DeviceCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentDevices_EquipmentId",
                table: "EquipmentDevices",
                column: "EquipmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemberAccessCards_UidHash",
                table: "MemberAccessCards",
                column: "UidHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemberAccessCards_UserId",
                table: "MemberAccessCards",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Equipment_AspNetUsers_OwnerUserId",
                table: "Equipment",
                column: "OwnerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Equipment_AspNetUsers_PartnerUserId",
                table: "Equipment",
                column: "PartnerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Equipment_AspNetUsers_OwnerUserId",
                table: "Equipment");

            migrationBuilder.DropForeignKey(
                name: "FK_Equipment_AspNetUsers_PartnerUserId",
                table: "Equipment");

            migrationBuilder.DropTable(
                name: "EquipmentDevices");

            migrationBuilder.DropTable(
                name: "MemberAccessCards");

            migrationBuilder.DropIndex(
                name: "IX_Equipment_OwnerUserId",
                table: "Equipment");

            migrationBuilder.DropIndex(
                name: "IX_Equipment_PartnerUserId",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "FaultReports");

            migrationBuilder.DropColumn(
                name: "Comment",
                table: "FaultReports");

            migrationBuilder.DropColumn(
                name: "IsSeriousSafetyConcern",
                table: "FaultReports");

            migrationBuilder.DropColumn(
                name: "MaintenanceNotes",
                table: "FaultReports");

            migrationBuilder.DropColumn(
                name: "EventType",
                table: "EquipmentStatusEvents");

            migrationBuilder.DropColumn(
                name: "PreviousState",
                table: "EquipmentStatusEvents");

            migrationBuilder.DropColumn(
                name: "ClearanceGraceStartedAt",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "PartnerUserId",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "PresenceChangedAt",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "PresenceDetected",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "StateBeforeOffline",
                table: "Equipment");

            migrationBuilder.RenameColumn(
                name: "ResolutionDate",
                table: "FaultReports",
                newName: "ResolvedAt");
        }
    }
}
