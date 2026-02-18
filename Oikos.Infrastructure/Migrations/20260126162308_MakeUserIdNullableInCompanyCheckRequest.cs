using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeUserIdNullableInCompanyCheckRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "CompanyCheckRequests",
                type: "INTEGER",
                nullable: true,
                comment: "User identifier",
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldComment: "User identifier");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "CompanyCheckRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                comment: "User identifier",
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true,
                oldComment: "User identifier");
        }
    }
}
