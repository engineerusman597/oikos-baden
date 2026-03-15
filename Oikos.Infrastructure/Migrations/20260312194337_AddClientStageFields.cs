using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClientStageFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientName",
                table: "InvoiceStages",
                type: "TEXT",
                maxLength: 150,
                nullable: true,
                comment: "Client-facing stage name (English)");

            migrationBuilder.AddColumn<string>(
                name: "ClientNameDe",
                table: "InvoiceStages",
                type: "TEXT",
                maxLength: 150,
                nullable: true,
                comment: "Client-facing stage name (German)");

            migrationBuilder.AddColumn<bool>(
                name: "RequiresClientAction",
                table: "InvoiceStages",
                type: "INTEGER",
                nullable: false,
                defaultValue: false,
                comment: "When true, the client must take an action (e.g. commission enforcement) to advance the case");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientName",
                table: "InvoiceStages");

            migrationBuilder.DropColumn(
                name: "ClientNameDe",
                table: "InvoiceStages");

            migrationBuilder.DropColumn(
                name: "RequiresClientAction",
                table: "InvoiceStages");
        }
    }
}
