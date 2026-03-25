using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShelterApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAnimalCharacteristics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GoodWithAnimals",
                table: "Animals");

            migrationBuilder.DropColumn(
                name: "GoodWithChildren",
                table: "Animals");

            migrationBuilder.RenameColumn(
                name: "RequiredExperience",
                table: "Animals",
                newName: "ChildrenCompatibility");

            migrationBuilder.AddColumn<string>(
                name: "AnimalCompatibility",
                table: "Animals",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CareTime",
                table: "Animals",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnimalCompatibility",
                table: "Animals");

            migrationBuilder.DropColumn(
                name: "CareTime",
                table: "Animals");

            migrationBuilder.RenameColumn(
                name: "ChildrenCompatibility",
                table: "Animals",
                newName: "RequiredExperience");

            migrationBuilder.AddColumn<bool>(
                name: "GoodWithAnimals",
                table: "Animals",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "GoodWithChildren",
                table: "Animals",
                type: "boolean",
                nullable: true);
        }
    }
}
