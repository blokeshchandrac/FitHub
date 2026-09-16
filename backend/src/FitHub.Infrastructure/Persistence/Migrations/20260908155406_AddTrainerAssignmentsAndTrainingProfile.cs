using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainerAssignmentsAndTrainingProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "WorkoutTemplates",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Personal");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndedAt",
                table: "WorkoutPlanAssignments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExperienceLevel",
                table: "AspNetUsers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlannedTrainingDays",
                table: "AspNetUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "PreferredGymBranchId",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "WorkoutTemplates" AS template
                SET "Kind" = 'TrainerPlan', "IsPublic" = FALSE
                WHERE template."OwnerUserId" IN (
                    SELECT user_role."UserId"
                    FROM "AspNetUserRoles" AS user_role
                    INNER JOIN "AspNetRoles" AS role ON role."Id" = user_role."RoleId"
                    WHERE role."Name" = 'Trainer');

                UPDATE "AspNetUsers" AS member
                SET "OnboardingCompleted" = FALSE
                WHERE member."Id" IN (
                    SELECT user_role."UserId"
                    FROM "AspNetUserRoles" AS user_role
                    INNER JOIN "AspNetRoles" AS role ON role."Id" = user_role."RoleId"
                    WHERE role."Name" = 'Member');
                """);

            migrationBuilder.CreateTable(
                name: "GymBranches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymBranches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrainerMemberAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainerId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AssignedByAdministratorId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnassignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UnassignedByAdministratorId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainerMemberAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainerMemberAssignments_AspNetUsers_AssignedByAdministrato~",
                        column: x => x.AssignedByAdministratorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainerMemberAssignments_AspNetUsers_MemberId",
                        column: x => x.MemberId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainerMemberAssignments_AspNetUsers_TrainerId",
                        column: x => x.TrainerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainerMemberAssignments_AspNetUsers_UnassignedByAdministra~",
                        column: x => x.UnassignedByAdministratorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutTemplates_OwnerUserId_Kind",
                table: "WorkoutTemplates",
                columns: new[] { "OwnerUserId", "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_PreferredGymBranchId",
                table: "AspNetUsers",
                column: "PreferredGymBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_GymBranches_Code",
                table: "GymBranches",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GymBranches_IsActive",
                table: "GymBranches",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TrainerMemberAssignments_AssignedByAdministratorId",
                table: "TrainerMemberAssignments",
                column: "AssignedByAdministratorId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainerMemberAssignments_MemberId",
                table: "TrainerMemberAssignments",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainerMemberAssignments_TrainerId",
                table: "TrainerMemberAssignments",
                column: "TrainerId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainerMemberAssignments_TrainerId_MemberId",
                table: "TrainerMemberAssignments",
                columns: new[] { "TrainerId", "MemberId" },
                unique: true,
                filter: "\"Status\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_TrainerMemberAssignments_UnassignedByAdministratorId",
                table: "TrainerMemberAssignments",
                column: "UnassignedByAdministratorId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_GymBranches_PreferredGymBranchId",
                table: "AspNetUsers",
                column: "PreferredGymBranchId",
                principalTable: "GymBranches",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_GymBranches_PreferredGymBranchId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "GymBranches");

            migrationBuilder.DropTable(
                name: "TrainerMemberAssignments");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutTemplates_OwnerUserId_Kind",
                table: "WorkoutTemplates");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_PreferredGymBranchId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "WorkoutTemplates");

            migrationBuilder.DropColumn(
                name: "EndedAt",
                table: "WorkoutPlanAssignments");

            migrationBuilder.DropColumn(
                name: "ExperienceLevel",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PlannedTrainingDays",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PreferredGymBranchId",
                table: "AspNetUsers");
        }
    }
}
