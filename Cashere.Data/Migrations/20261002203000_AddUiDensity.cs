using Cashere.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cashere.Data.Migrations;

[DbContext(typeof(CashereDbContext))]
[Migration("20261002203000_AddUiDensity")]
public partial class AddUiDensity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<string>(name: "UiDensity", table: "ReceiptAdmin", type: "TEXT", maxLength: 20, nullable: false, defaultValue: "Comfortable");

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn("UiDensity", "ReceiptAdmin");
}
