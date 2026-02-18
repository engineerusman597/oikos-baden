using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionIdToUserSubscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SubscriptionId",
                table: "UserSubscriptions",
                type: "INTEGER",
                nullable: true,
                comment: "Foreign key to the purchase record");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_SubscriptionId",
                table: "UserSubscriptions",
                column: "SubscriptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSubscriptions_Subscriptions_SubscriptionId",
                table: "UserSubscriptions",
                column: "SubscriptionId",
                principalTable: "Subscriptions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserSubscriptions_Subscriptions_SubscriptionId",
                table: "UserSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_UserSubscriptions_SubscriptionId",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "SubscriptionId",
                table: "UserSubscriptions");
        }
    }
}
