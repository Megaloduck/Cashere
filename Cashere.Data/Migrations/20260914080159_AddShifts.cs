using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cashere.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShifts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Shifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CashierId = table.Column<int>(type: "INTEGER", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    StartingCash = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ExpectedCashAtClose = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    ActualCashAtClose = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    DiscrepancyAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Shifts_Cashiers_CashierId",
                        column: x => x.CashierId,
                        principalTable: "Cashiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_CashierId",
                table: "Shifts",
                column: "CashierId");

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_OpenedAt",
                table: "Shifts",
                column: "OpenedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Shifts");
        }
    }
}
