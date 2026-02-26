using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InvoiceClientDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InvoiceId = table.Column<int>(type: "INTEGER", nullable: false, comment: "Invoice this document belongs to"),
                    UploadedByUserId = table.Column<int>(type: "INTEGER", nullable: false, comment: "User who uploaded this document"),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false, comment: "Original file name"),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false, comment: "Stored file path (relative to web root)"),
                    UploadedAt = table.Column<DateTime>(type: "TEXT", nullable: false, comment: "Upload time")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceClientDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceClientDocuments_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Documents uploaded by the client for a specific invoice");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceClientDocuments_InvoiceId",
                table: "InvoiceClientDocuments",
                column: "InvoiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceClientDocuments");
        }
    }
}
