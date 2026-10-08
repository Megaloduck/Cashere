using Cashere.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cashere.Data.Migrations;

[DbContext(typeof(CashereDbContext))]
[Migration("20261002173000_AddSaleNumberFormattingSettings")]
public partial class AddSaleNumberFormattingSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "SaleNumberPrefix",
            table: "ReceiptAdmin",
            type: "TEXT",
            maxLength: 10,
            nullable: false,
            defaultValue: "S");

        migrationBuilder.AddColumn<bool>(
            name: "IncludeDateInSaleNumber",
            table: "ReceiptAdmin",
            type: "INTEGER",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<int>(
            name: "SaleNumberSequenceDigits",
            table: "ReceiptAdmin",
            type: "INTEGER",
            nullable: false,
            defaultValue: 4);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("SaleNumberPrefix", "ReceiptAdmin");
        migrationBuilder.DropColumn("IncludeDateInSaleNumber", "ReceiptAdmin");
        migrationBuilder.DropColumn("SaleNumberSequenceDigits", "ReceiptAdmin");
    }
}
