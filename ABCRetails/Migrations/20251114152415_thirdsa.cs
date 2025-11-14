using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ABCRetails.Migrations
{
    /// <inheritdoc />
    public partial class thirdsa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductName",
                table: "Cart");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                table: "Cart",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
