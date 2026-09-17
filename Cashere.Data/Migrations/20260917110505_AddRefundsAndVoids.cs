using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cashere.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundsAndVoids : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Refunds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SaleId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProcessedByCashierId = table.Column<int>(type: "INTEGER", nullable: false),
                    RefundDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsVoid = table.Column<bool>(type: "INTEGER", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Refunds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Refunds_Cashiers_ProcessedByCashierId",
                        column: x => x.ProcessedByCashierId,
                        principalTable: "Cashiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Refunds_Sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefundLineItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RefundId = table.Column<int>(type: "INTEGER", nullable: false),
                    SaleItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Subtotal = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefundLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefundLineItems_Refunds_RefundId",
                        column: x => x.RefundId,
                        principalTable: "Refunds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RefundLineItems_SaleItems_SaleItemId",
                        column: x => x.SaleItemId,
                        principalTable: "SaleItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefundLineItems_RefundId",
                table: "RefundLineItems",
                column: "RefundId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundLineItems_SaleItemId",
                table: "RefundLineItems",
                column: "SaleItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_ProcessedByCashierId",
                table: "Refunds",
                column: "ProcessedByCashierId");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_RefundDate",
                table: "Refunds",
                column: "RefundDate");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_SaleId",
                table: "Refunds",
                column: "SaleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefundLineItems");

            migrationBuilder.DropTable(
                name: "Refunds");
        }
    }
}
