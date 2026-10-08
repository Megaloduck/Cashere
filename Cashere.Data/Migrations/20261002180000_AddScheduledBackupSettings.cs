using Cashere.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cashere.Data.Migrations;

[DbContext(typeof(CashereDbContext))]
[Migration("20261002180000_AddScheduledBackupSettings")]
public partial class AddScheduledBackupSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(name: "ScheduledBackupsEnabled", table: "ReceiptAdmin", type: "INTEGER", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<string>(name: "ScheduledBackupTime", table: "ReceiptAdmin", type: "TEXT", maxLength: 5, nullable: false, defaultValue: "23:00");
        migrationBuilder.AddColumn<int>(name: "AutomaticBackupRetentionCount", table: "ReceiptAdmin", type: "INTEGER", nullable: false, defaultValue: 30);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("ScheduledBackupsEnabled", "ReceiptAdmin");
        migrationBuilder.DropColumn("ScheduledBackupTime", "ReceiptAdmin");
        migrationBuilder.DropColumn("AutomaticBackupRetentionCount", "ReceiptAdmin");
    }
}
