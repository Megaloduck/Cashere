using Cashere.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cashere.Data.Migrations;

[DbContext(typeof(CashereDbContext))]
[Migration("20261002190000_AddCashierActionPermissions")]
public partial class AddCashierActionPermissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(name: "CashierCanViewOwnSalesHistory", table: "ReceiptAdmin", type: "INTEGER", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<bool>(name: "CashierCanRequestRefunds", table: "ReceiptAdmin", type: "INTEGER", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<bool>(name: "CashierCanRequestVoids", table: "ReceiptAdmin", type: "INTEGER", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<bool>(name: "CashierCanApplyVouchers", table: "ReceiptAdmin", type: "INTEGER", nullable: false, defaultValue: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("CashierCanViewOwnSalesHistory", "ReceiptAdmin");
        migrationBuilder.DropColumn("CashierCanRequestRefunds", "ReceiptAdmin");
        migrationBuilder.DropColumn("CashierCanRequestVoids", "ReceiptAdmin");
        migrationBuilder.DropColumn("CashierCanApplyVouchers", "ReceiptAdmin");
    }
}
