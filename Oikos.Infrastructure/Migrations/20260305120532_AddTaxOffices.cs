using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxOffices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaxOffices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false, comment: "Primary key")
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false, comment: "Tax office display name"),
                    BusinessName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true, comment: "Official business/company name"),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true, comment: "Contact email"),
                    PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true, comment: "Contact phone number"),
                    ContactPerson = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true, comment: "Name of the contact person"),
                    Address = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true, comment: "Business address"),
                    PostalCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true, comment: "Postal code"),
                    City = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true, comment: "City"),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true, comment: "Internal notes"),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, comment: "Unique referral code (STB-xxxx format)"),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, comment: "Whether the tax office is active"),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, comment: "Creation timestamp (UTC)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxOffices", x => x.Id);
                },
                comment: "Tax offices / Steuerberaterbüros");

            migrationBuilder.CreateIndex(
                name: "IX_TaxOffices_Code",
                table: "TaxOffices",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaxOffices");
        }
    }
}
