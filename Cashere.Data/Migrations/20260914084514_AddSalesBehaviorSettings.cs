using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cashere.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesBehaviorSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoPrintReceiptAfterPayment",
                table: "ReceiptAdmin",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequireCustomerBeforeCheckout",
                table: "ReceiptAdmin",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoPrintReceiptAfterPayment",
                table: "ReceiptAdmin");

            migrationBuilder.DropColumn(
                name: "RequireCustomerBeforeCheckout",
                table: "ReceiptAdmin");
        }
    }
}
