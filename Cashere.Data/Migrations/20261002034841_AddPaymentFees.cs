using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cashere.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentFees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CashFeePercent",
                table: "ReceiptAdmin",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "EdcFeePercent",
                table: "ReceiptAdmin",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "QrisFeePercent",
                table: "ReceiptAdmin",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FeeAmount",
                table: "Payments",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CashFeePercent",
                table: "ReceiptAdmin");

            migrationBuilder.DropColumn(
                name: "EdcFeePercent",
                table: "ReceiptAdmin");

            migrationBuilder.DropColumn(
                name: "QrisFeePercent",
                table: "ReceiptAdmin");

            migrationBuilder.DropColumn(
                name: "FeeAmount",
                table: "Payments");
        }
    }
}
