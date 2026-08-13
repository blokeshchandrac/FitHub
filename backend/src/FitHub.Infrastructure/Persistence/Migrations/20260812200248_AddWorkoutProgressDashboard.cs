using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkoutProgressDashboard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Equipment",
                table: "Exercises",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HeightCm",
                table: "BodyMeasurements",
                type: "numeric(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WorkoutPlanAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkoutTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutPlanAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkoutPlanAssignments_AspNetUsers_MemberUserId",
                        column: x => x.MemberUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkoutPlanAssignments_AspNetUsers_TrainerUserId",
                        column: x => x.TrainerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkoutPlanAssignments_WorkoutTemplates_WorkoutTemplateId",
                        column: x => x.WorkoutTemplateId,
                        principalTable: "WorkoutTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_Equipment",
                table: "Exercises",
                column: "Equipment");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutPlanAssignments_MemberUserId_IsActive",
                table: "WorkoutPlanAssignments",
                columns: new[] { "MemberUserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutPlanAssignments_TrainerUserId",
                table: "WorkoutPlanAssignments",
                column: "TrainerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutPlanAssignments_WorkoutTemplateId_MemberUserId_IsAct~",
                table: "WorkoutPlanAssignments",
                columns: new[] { "WorkoutTemplateId", "MemberUserId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkoutPlanAssignments");

            migrationBuilder.DropIndex(
                name: "IX_Exercises_Equipment",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "Equipment",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "HeightCm",
                table: "BodyMeasurements");
        }
    }
}
