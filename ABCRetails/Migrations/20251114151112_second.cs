using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ABCRetails.Migrations
{
    /// <inheritdoc />
    public partial class second : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cart_Products_ProductId",
                table: "Cart");

            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                table: "Cart",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_Cart_Products_ProductId",
                table: "Cart",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cart_Products_ProductId",
                table: "Cart");

            migrationBuilder.DropColumn(
                name: "ProductName",
                table: "Cart");

            migrationBuilder.AddForeignKey(
                name: "FK_Cart_Products_ProductId",
                table: "Cart",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
