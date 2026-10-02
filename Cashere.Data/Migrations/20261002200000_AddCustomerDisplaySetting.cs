using Cashere.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cashere.Data.Migrations;

[DbContext(typeof(CashereDbContext))]
[Migration("20261002200000_AddCustomerDisplaySetting")]
public partial class AddCustomerDisplaySetting : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<bool>(name: "EnableCustomerDisplay", table: "ReceiptAdmin", type: "INTEGER", nullable: false, defaultValue: false);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn("EnableCustomerDisplay", "ReceiptAdmin");
}
