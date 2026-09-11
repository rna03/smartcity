using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartCity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIncidentDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "incidents",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "incidents");
        }
    }
}
