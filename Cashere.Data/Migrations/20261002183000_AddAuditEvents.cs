using Cashere.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cashere.Data.Migrations;

[DbContext(typeof(CashereDbContext))]
[Migration("20261002183000_AddAuditEvents")]
public partial class AddAuditEvents : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AuditEvents",
            columns: table => new
            {
                Id = table.Column<long>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                OccurredAtUtc = table.Column<System.DateTime>(type: "TEXT", nullable: false),
                ActorCashierId = table.Column<int>(type: "INTEGER", nullable: true),
                ActorName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Action = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                EntityType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                EntityId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                Summary = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_AuditEvents", x => x.Id));

        migrationBuilder.CreateIndex(name: "IX_AuditEvents_ActorCashierId", table: "AuditEvents", column: "ActorCashierId");
        migrationBuilder.CreateIndex(name: "IX_AuditEvents_OccurredAtUtc", table: "AuditEvents", column: "OccurredAtUtc");
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "AuditEvents");
}
