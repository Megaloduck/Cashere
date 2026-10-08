using Cashere.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cashere.Data.Migrations;

[DbContext(typeof(CashereDbContext))]
[Migration("20261002134000_AddSkuBarcodeFormatSettings")]
public partial class AddSkuBarcodeFormatSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("SkuPrefix", "ReceiptAdmin", type: "TEXT", maxLength: 12, nullable: false, defaultValue: "SKU");
        migrationBuilder.AddColumn<int>("SkuNumberLength", "ReceiptAdmin", type: "INTEGER", nullable: false, defaultValue: 6);
        migrationBuilder.AddColumn<string>("InternalBarcodePrefix", "ReceiptAdmin", type: "TEXT", maxLength: 2, nullable: false, defaultValue: "20");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("SkuPrefix", "ReceiptAdmin");
        migrationBuilder.DropColumn("SkuNumberLength", "ReceiptAdmin");
        migrationBuilder.DropColumn("InternalBarcodePrefix", "ReceiptAdmin");
    }
}
