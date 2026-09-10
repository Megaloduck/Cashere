using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cashere.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddServerNetworkSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ServerBindAddress",
                table: "ShopSettings",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "0.0.0.0");

            migrationBuilder.AddColumn<int>(
                name: "ServerPort",
                table: "ShopSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 5177);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ServerBindAddress",
                table: "ShopSettings");

            migrationBuilder.DropColumn(
                name: "ServerPort",
                table: "ShopSettings");
        }
    }
}