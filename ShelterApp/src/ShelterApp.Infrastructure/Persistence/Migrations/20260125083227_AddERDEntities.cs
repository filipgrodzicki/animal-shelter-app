using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShelterApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddERDEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Animals_ChipNumber",
                table: "Animals");

            migrationBuilder.AddColumn<string>(
                name: "SpecialNeeds",
                table: "Animals",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ContractId",
                table: "AdoptionApplications",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AdoptionContracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdoptionApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GeneratedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SignedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ShelterSignatory = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AdopterSignatory = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AdoptionFee = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    FeePaid = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    FeePaidDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AdditionalTerms = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdoptionContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdoptionContracts_AdoptionApplications_AdoptionApplicationId",
                        column: x => x.AdoptionApplicationId,
                        principalTable: "AdoptionApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerCertificates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VolunteerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CertificateNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IssueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IssuingOrganization = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerCertificates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VolunteerCertificates_Volunteers_VolunteerId",
                        column: x => x.VolunteerId,
                        principalTable: "Volunteers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Animals_ChipNumber",
                table: "Animals",
                column: "ChipNumber",
                unique: true,
                filter: "\"ChipNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AdoptionContracts_AdoptionApplicationId",
                table: "AdoptionContracts",
                column: "AdoptionApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdoptionContracts_ContractNumber",
                table: "AdoptionContracts",
                column: "ContractNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdoptionContracts_SignedDate",
                table: "AdoptionContracts",
                column: "SignedDate");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerCertificates_CertificateNumber",
                table: "VolunteerCertificates",
                column: "CertificateNumber");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerCertificates_ExpiryDate",
                table: "VolunteerCertificates",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerCertificates_Type",
                table: "VolunteerCertificates",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerCertificates_VolunteerId",
                table: "VolunteerCertificates",
                column: "VolunteerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdoptionContracts");

            migrationBuilder.DropTable(
                name: "VolunteerCertificates");

            migrationBuilder.DropIndex(
                name: "IX_Animals_ChipNumber",
                table: "Animals");

            migrationBuilder.DropColumn(
                name: "SpecialNeeds",
                table: "Animals");

            migrationBuilder.DropColumn(
                name: "ContractId",
                table: "AdoptionApplications");

            migrationBuilder.CreateIndex(
                name: "IX_Animals_ChipNumber",
                table: "Animals",
                column: "ChipNumber");
        }
    }
}
