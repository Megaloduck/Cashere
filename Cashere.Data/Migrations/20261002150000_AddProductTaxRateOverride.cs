using Cashere.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cashere.Data.Migrations;

[DbContext(typeof(CashereDbContext))]
[Migration("20261002150000_AddProductTaxRateOverride")]
public partial class AddProductTaxRateOverride : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "TaxRateOverridePercent",
            table: "Products",
            type: "TEXT",
            precision: 5,
            scale: 2,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("TaxRateOverridePercent", "Products");
    }
}
