using Cashere.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cashere.Data.Migrations;

[DbContext(typeof(CashereDbContext))]
[Migration("20261002201500_AddOrderTypes")]
public partial class AddOrderTypes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "OrderTypes", table: "ReceiptAdmin", type: "TEXT", maxLength: 338, nullable: false, defaultValue: "Sale");
        migrationBuilder.AddColumn<string>(name: "OrderType", table: "Sales", type: "TEXT", maxLength: 32, nullable: false, defaultValue: "Sale");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("OrderTypes", "ReceiptAdmin");
        migrationBuilder.DropColumn("OrderType", "Sales");
    }
}
