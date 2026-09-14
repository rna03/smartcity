using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartCity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIncidentPriority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PriorityLevel",
                table: "incidents",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Low");

            migrationBuilder.AddColumn<int>(
                name: "PriorityScore",
                table: "incidents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "ck_incidents_priority_score",
                table: "incidents",
                sql: "\"PriorityScore\" BETWEEN 0 AND 100");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_incidents_priority_score",
                table: "incidents");

            migrationBuilder.DropColumn(
                name: "PriorityLevel",
                table: "incidents");

            migrationBuilder.DropColumn(
                name: "PriorityScore",
                table: "incidents");
        }
    }
}
