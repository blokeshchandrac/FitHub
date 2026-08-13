using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddParkingAndRuleBasedRecommendations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Note",
                table: "ParkingStatusEvents",
                newName: "Reason");

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "Recommendations",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "Created before Stage 7 rule tracking.");

            migrationBuilder.AddColumn<string>(
                name: "RuleCode",
                table: "Recommendations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "LEGACY_RECOMMENDATION");

            migrationBuilder.AddColumn<decimal>(
                name: "Confidence",
                table: "ParkingStatusEvents",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<string>(
                name: "EventType",
                table: "ParkingStatusEvents",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "LegacyStatusChange");

            migrationBuilder.AddColumn<string>(
                name: "PreviousState",
                table: "ParkingStatusEvents",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Available");

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "ParkingStatusEvents",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "Legacy");

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentConfidence",
                table: "ParkingSpaces",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<string>(
                name: "CurrentSource",
                table: "ParkingSpaces",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "Legacy");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastEventAt",
                table: "ParkingSpaces",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "ActivityLevel",
                table: "AspNetUsers",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Moderate");

            migrationBuilder.AddColumn<string>(
                name: "FitnessGoal",
                table: "AspNetUsers",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "GeneralFitness");

            migrationBuilder.Sql("UPDATE \"ParkingSpaces\" SET \"CurrentState\" = CASE WHEN \"CurrentState\" = 'Reserved' THEN 'Unknown' WHEN \"CurrentState\" = 'OutOfService' THEN 'Disabled' ELSE \"CurrentState\" END, \"LastEventAt\" = \"UpdatedAt\";");
            migrationBuilder.Sql("UPDATE \"ParkingStatusEvents\" SET \"State\" = CASE WHEN \"State\" = 'Reserved' THEN 'Unknown' WHEN \"State\" = 'OutOfService' THEN 'Disabled' ELSE \"State\" END;");
            migrationBuilder.Sql("UPDATE \"ParkingStatusEvents\" SET \"PreviousState\" = \"State\";");

            migrationBuilder.CreateTable(
                name: "ParkingCameras",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CameraCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CredentialHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    LastEventAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingCameras", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParkingCameras_CameraCode",
                table: "ParkingCameras",
                column: "CameraCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParkingCameras");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "Recommendations");

            migrationBuilder.DropColumn(
                name: "RuleCode",
                table: "Recommendations");

            migrationBuilder.DropColumn(
                name: "Confidence",
                table: "ParkingStatusEvents");

            migrationBuilder.DropColumn(
                name: "EventType",
                table: "ParkingStatusEvents");

            migrationBuilder.DropColumn(
                name: "PreviousState",
                table: "ParkingStatusEvents");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "ParkingStatusEvents");

            migrationBuilder.DropColumn(
                name: "CurrentConfidence",
                table: "ParkingSpaces");

            migrationBuilder.DropColumn(
                name: "CurrentSource",
                table: "ParkingSpaces");

            migrationBuilder.DropColumn(
                name: "LastEventAt",
                table: "ParkingSpaces");

            migrationBuilder.DropColumn(
                name: "ActivityLevel",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "FitnessGoal",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "Reason",
                table: "ParkingStatusEvents",
                newName: "Note");
        }
    }
}
