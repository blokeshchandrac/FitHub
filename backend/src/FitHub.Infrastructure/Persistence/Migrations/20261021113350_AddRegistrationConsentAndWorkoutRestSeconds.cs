using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationConsentAndWorkoutRestSeconds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RestSeconds",
                table: "SetLogs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ConsentAcceptedAtUtc",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConsentVersion",
                table: "AspNetUsers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_SetLogs_RestSeconds",
                table: "SetLogs",
                sql: "\"RestSeconds\" IS NULL OR (\"RestSeconds\" >= 0 AND \"RestSeconds\" <= 3600)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_SetLogs_RestSeconds",
                table: "SetLogs");

            migrationBuilder.DropColumn(
                name: "RestSeconds",
                table: "SetLogs");

            migrationBuilder.DropColumn(
                name: "ConsentAcceptedAtUtc",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ConsentVersion",
                table: "AspNetUsers");
        }
    }
}
