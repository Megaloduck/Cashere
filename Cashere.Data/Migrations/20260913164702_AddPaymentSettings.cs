using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cashere.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CashEnabled",
                table: "ReceiptAdmin",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "EdcAccountInfo",
                table: "ReceiptAdmin",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EdcEnabled",
                table: "ReceiptAdmin",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "QrisAccountInfo",
                table: "ReceiptAdmin",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "QrisEnabled",
                table: "ReceiptAdmin",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequireConfirmationForNonCash",
                table: "ReceiptAdmin",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CashEnabled",
                table: "ReceiptAdmin");

            migrationBuilder.DropColumn(
                name: "EdcAccountInfo",
                table: "ReceiptAdmin");

            migrationBuilder.DropColumn(
                name: "EdcEnabled",
                table: "ReceiptAdmin");

            migrationBuilder.DropColumn(
                name: "QrisAccountInfo",
                table: "ReceiptAdmin");

            migrationBuilder.DropColumn(
                name: "QrisEnabled",
                table: "ReceiptAdmin");

            migrationBuilder.DropColumn(
                name: "RequireConfirmationForNonCash",
                table: "ReceiptAdmin");
        }
    }
}
