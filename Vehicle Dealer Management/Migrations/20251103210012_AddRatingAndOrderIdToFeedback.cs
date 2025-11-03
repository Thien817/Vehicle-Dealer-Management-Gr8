using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vehicle_Dealer_Management.Migrations
{
    /// <inheritdoc />
    public partial class AddRatingAndOrderIdToFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrderId",
                table: "Feedbacks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Rating",
                table: "Feedbacks",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_DealerId_Type_Rating",
                table: "Feedbacks",
                columns: new[] { "DealerId", "Type", "Rating" });

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_OrderId",
                table: "Feedbacks",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_Type_OrderId",
                table: "Feedbacks",
                columns: new[] { "Type", "OrderId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Feedbacks_SalesDocuments_OrderId",
                table: "Feedbacks",
                column: "OrderId",
                principalTable: "SalesDocuments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Feedbacks_SalesDocuments_OrderId",
                table: "Feedbacks");

            migrationBuilder.DropIndex(
                name: "IX_Feedbacks_DealerId_Type_Rating",
                table: "Feedbacks");

            migrationBuilder.DropIndex(
                name: "IX_Feedbacks_OrderId",
                table: "Feedbacks");

            migrationBuilder.DropIndex(
                name: "IX_Feedbacks_Type_OrderId",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Feedbacks");
        }
    }
}
