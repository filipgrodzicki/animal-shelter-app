using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShelterApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExperienceLevelToAnimal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ActivityLevel",
                table: "Animals",
                newName: "ExperienceLevel");

            migrationBuilder.AddColumn<string>(
                name: "AvailableCareTime",
                table: "AdoptionApplications",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExperienceLevelApplicant",
                table: "AdoptionApplications",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasChildren",
                table: "AdoptionApplications",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasOtherAnimals",
                table: "AdoptionApplications",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HousingType",
                table: "AdoptionApplications",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvailableCareTime",
                table: "AdoptionApplications");

            migrationBuilder.DropColumn(
                name: "ExperienceLevelApplicant",
                table: "AdoptionApplications");

            migrationBuilder.DropColumn(
                name: "HasChildren",
                table: "AdoptionApplications");

            migrationBuilder.DropColumn(
                name: "HasOtherAnimals",
                table: "AdoptionApplications");

            migrationBuilder.DropColumn(
                name: "HousingType",
                table: "AdoptionApplications");

            migrationBuilder.RenameColumn(
                name: "ExperienceLevel",
                table: "Animals",
                newName: "ActivityLevel");
        }
    }
}
