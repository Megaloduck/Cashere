using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cashere.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHeaderClockSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowHeaderClock",
                table: "ReceiptAdmin",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowHeaderDate",
                table: "ReceiptAdmin",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowHeaderDay",
                table: "ReceiptAdmin",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowHeaderHours",
                table: "ReceiptAdmin",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowHeaderMonth",
                table: "ReceiptAdmin",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowHeaderYear",
                table: "ReceiptAdmin",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowHeaderClock",
                table: "ReceiptAdmin");

            migrationBuilder.DropColumn(
                name: "ShowHeaderDate",
                table: "ReceiptAdmin");

            migrationBuilder.DropColumn(
                name: "ShowHeaderDay",
                table: "ReceiptAdmin");

            migrationBuilder.DropColumn(
                name: "ShowHeaderHours",
                table: "ReceiptAdmin");

            migrationBuilder.DropColumn(
                name: "ShowHeaderMonth",
                table: "ReceiptAdmin");

            migrationBuilder.DropColumn(
                name: "ShowHeaderYear",
                table: "ReceiptAdmin");
        }
    }
}
