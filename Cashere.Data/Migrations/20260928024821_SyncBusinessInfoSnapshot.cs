using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cashere.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncBusinessInfoSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RoundingAdjustment",
                table: "Sales",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                table: "SaleItems",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "BusinessHoursJson",
                table: "ReceiptAdmin",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoPath",
                table: "ReceiptAdmin",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PricesIncludeTax",
                table: "ReceiptAdmin",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "RoundingIncrement",
                table: "ReceiptAdmin",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "RoundingMode",
                table: "ReceiptAdmin",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TaxRateId",
                table: "Categories",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TaxRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RatePercent = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxRates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_TaxRateId",
                table: "Categories",
                column: "TaxRateId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxRates_Name",
                table: "TaxRates",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_TaxRates_TaxRateId",
                table: "Categories",
                column: "TaxRateId",
                principalTable: "TaxRates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_TaxRates_TaxRateId",
                table: "Categories");

            migrationBuilder.DropTable(
                name: "TaxRates");

            migrationBuilder.DropIndex(
                name: "IX_Categories_TaxRateId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "RoundingAdjustment",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "BusinessHoursJson",
                table: "ReceiptAdmin");

            migrationBuilder.DropColumn(
                name: "LogoPath",
                table: "ReceiptAdmin");

            migrationBuilder.DropColumn(
                name: "PricesIncludeTax",
                table: "ReceiptAdmin");

            migrationBuilder.DropColumn(
                name: "RoundingIncrement",
                table: "ReceiptAdmin");

            migrationBuilder.DropColumn(
                name: "RoundingMode",
                table: "ReceiptAdmin");

            migrationBuilder.DropColumn(
                name: "TaxRateId",
                table: "Categories");
        }
    }
}
