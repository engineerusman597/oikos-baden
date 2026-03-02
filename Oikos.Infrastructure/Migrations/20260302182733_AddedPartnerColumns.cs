using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedPartnerColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Partners",
                type: "TEXT",
                maxLength: 500,
                nullable: true,
                comment: "Business address");

            migrationBuilder.AddColumn<string>(
                name: "BusinessName",
                table: "Partners",
                type: "TEXT",
                maxLength: 200,
                nullable: true,
                comment: "Official business/company name");

            migrationBuilder.AddColumn<int>(
                name: "CommissionPeriodMonths",
                table: "Partners",
                type: "INTEGER",
                nullable: true,
                comment: "Months commissions are paid after membership start (null = unlimited)");

            migrationBuilder.AddColumn<string>(
                name: "ContactPerson",
                table: "Partners",
                type: "TEXT",
                maxLength: 200,
                nullable: true,
                comment: "Name of the contact person at the partner");

            migrationBuilder.AddColumn<string>(
                name: "PartnerType",
                table: "Partners",
                type: "TEXT",
                maxLength: 50,
                nullable: true,
                comment: "Partner type: Rechtsanwalt, Steuerberater, Notar, Unternehmensberater, Sonstige");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "BusinessName",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "CommissionPeriodMonths",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "ContactPerson",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "PartnerType",
                table: "Partners");
        }
    }
}
