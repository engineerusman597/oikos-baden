using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchReasonToCompanyCheckRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SearchReason",
                table: "CompanyCheckRequests",
                type: "TEXT",
                maxLength: 50,
                nullable: true,
                comment: "Business purpose for the company search");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SearchReason",
                table: "CompanyCheckRequests");
        }
    }
}
