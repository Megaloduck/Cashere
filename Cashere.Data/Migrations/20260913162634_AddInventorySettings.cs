using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cashere.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInventorySettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoGenerateBarcode",
                table: "ReceiptAdmin",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AutoGenerateSku",
                table: "ReceiptAdmin",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DefaultLowStockThreshold",
                table: "ReceiptAdmin",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OutOfStockBehavior",
                table: "ReceiptAdmin",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "TrackInventory",
                table: "ReceiptAdmin",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoGenerateBarcode",
                table: "ReceiptAdmin");

            migrationBuilder.DropColumn(
                name: "AutoGenerateSku",
                table: "ReceiptAdmin");

            migrationBuilder.DropColumn(
                name: "DefaultLowStockThreshold",
                table: "ReceiptAdmin");

            migrationBuilder.DropColumn(
                name: "OutOfStockBehavior",
                table: "ReceiptAdmin");

            migrationBuilder.DropColumn(
                name: "TrackInventory",
                table: "ReceiptAdmin");
        }
    }
}
