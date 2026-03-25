using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShelterApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAnimalSurrenderAndDistinguishingMarksFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DistinguishingMarks",
                table: "Animals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SurrenderedByFirstName",
                table: "Animals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SurrenderedByLastName",
                table: "Animals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SurrenderedByPhone",
                table: "Animals",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DistinguishingMarks",
                table: "Animals");

            migrationBuilder.DropColumn(
                name: "SurrenderedByFirstName",
                table: "Animals");

            migrationBuilder.DropColumn(
                name: "SurrenderedByLastName",
                table: "Animals");

            migrationBuilder.DropColumn(
                name: "SurrenderedByPhone",
                table: "Animals");
        }
    }
}
