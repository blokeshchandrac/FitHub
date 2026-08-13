using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMembershipPaymentsAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                table: "Payments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MembershipPlanId",
                table: "Payments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "PurchaseKind",
                table: "Payments",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AttendanceCheckInTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceCheckInTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceCheckInTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProcessedPaymentWebhooks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    EventId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EventType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedPaymentWebhooks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_MembershipPlanId",
                table: "Payments",
                column: "MembershipPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceCheckInTokens_TokenHash",
                table: "AttendanceCheckInTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceCheckInTokens_UserId_ExpiresAt",
                table: "AttendanceCheckInTokens",
                columns: new[] { "UserId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedPaymentWebhooks_ProcessedAt",
                table: "ProcessedPaymentWebhooks",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedPaymentWebhooks_Provider_EventId",
                table: "ProcessedPaymentWebhooks",
                columns: new[] { "Provider", "EventId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_MembershipPlans_MembershipPlanId",
                table: "Payments",
                column: "MembershipPlanId",
                principalTable: "MembershipPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_MembershipPlans_MembershipPlanId",
                table: "Payments");

            migrationBuilder.DropTable(
                name: "AttendanceCheckInTokens");

            migrationBuilder.DropTable(
                name: "ProcessedPaymentWebhooks");

            migrationBuilder.DropIndex(
                name: "IX_Payments_MembershipPlanId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "MembershipPlanId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PurchaseKind",
                table: "Payments");
        }
    }
}
