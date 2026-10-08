using Cashere.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cashere.Data.Migrations;

[DbContext(typeof(CashereDbContext))]
[Migration("20261002213000_AddPosFeedbackPreferences")]
public partial class AddPosFeedbackPreferences : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(name: "PosSoundsEnabled", table: "ReceiptAdmin", type: "INTEGER", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<bool>(name: "ShowPosNotifications", table: "ReceiptAdmin", type: "INTEGER", nullable: false, defaultValue: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("PosSoundsEnabled", "ReceiptAdmin");
        migrationBuilder.DropColumn("ShowPosNotifications", "ReceiptAdmin");
    }
}
