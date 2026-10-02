using Cashere.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cashere.Data.Migrations;

[DbContext(typeof(CashereDbContext))]
[Migration("20261002193000_AddBarcodeScannerSetting")]
public partial class AddBarcodeScannerSetting : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<bool>(name: "AutoAddScannedBarcode", table: "ReceiptAdmin", type: "INTEGER", nullable: false, defaultValue: true);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn("AutoAddScannedBarcode", "ReceiptAdmin");
}
