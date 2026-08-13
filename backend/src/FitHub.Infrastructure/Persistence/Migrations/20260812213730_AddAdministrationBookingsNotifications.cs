using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdministrationBookingsNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeduplicationKey",
                table: "Notifications",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.Sql("UPDATE \"Notifications\" SET \"DeduplicationKey\" = 'legacy:' || \"Id\"::text;");
            migrationBuilder.Sql("UPDATE \"Notifications\" SET \"Type\" = 'WorkoutConsistency' WHERE \"Type\" NOT IN ('MembershipExpiry','ClassReminder','WorkoutConsistency','StreakWarning','PersonalRecord','FaultReportStatusUpdate');");

            migrationBuilder.AlterColumn<string>(
                name: "DeduplicationKey",
                table: "Notifications",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_DeduplicationKey",
                table: "Notifications",
                columns: new[] { "UserId", "DeduplicationKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_DeduplicationKey",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "DeduplicationKey",
                table: "Notifications");
        }
    }
}
