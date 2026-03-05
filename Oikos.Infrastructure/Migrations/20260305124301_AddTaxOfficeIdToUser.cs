using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxOfficeIdToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TaxOfficeId",
                table: "Users",
                type: "INTEGER",
                nullable: true,
                comment: "Linked tax office id");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TaxOfficeId",
                table: "Users",
                column: "TaxOfficeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_TaxOffices_TaxOfficeId",
                table: "Users",
                column: "TaxOfficeId",
                principalTable: "TaxOffices",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_TaxOffices_TaxOfficeId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_TaxOfficeId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TaxOfficeId",
                table: "Users");
        }
    }
}
