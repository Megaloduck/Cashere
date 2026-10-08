using Cashere.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cashere.Data.Migrations;

[DbContext(typeof(CashereDbContext))]
[Migration("20261008120000_AddAuditUndoAndRetention")]
public partial class AddAuditUndoAndRetention : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "BeforeValuesJson", table: "AuditEvents", type: "TEXT", nullable: true);
        migrationBuilder.AddColumn<string>(name: "AfterValuesJson", table: "AuditEvents", type: "TEXT", nullable: true);
        migrationBuilder.AddColumn<bool>(name: "IsUndone", table: "AuditEvents", type: "INTEGER", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<bool>(name: "IsReversible", table: "AuditEvents", type: "INTEGER", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<bool>(name: "AutoDeleteAuditEnabled", table: "ReceiptAdmin", type: "INTEGER", nullable: false, defaultValue: true);
        migrationBuilder.AddColumn<int>(name: "AuditRetentionMonths", table: "ReceiptAdmin", type: "INTEGER", nullable: false, defaultValue: 1);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "BeforeValuesJson", table: "AuditEvents");
        migrationBuilder.DropColumn(name: "AfterValuesJson", table: "AuditEvents");
        migrationBuilder.DropColumn(name: "IsUndone", table: "AuditEvents");
        migrationBuilder.DropColumn(name: "IsReversible", table: "AuditEvents");
        migrationBuilder.DropColumn(name: "AutoDeleteAuditEnabled", table: "ReceiptAdmin");
        migrationBuilder.DropColumn(name: "AuditRetentionMonths", table: "ReceiptAdmin");
    }
}
