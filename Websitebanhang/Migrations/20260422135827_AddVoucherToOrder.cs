using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Websitebanhang.Migrations
{
    /// <inheritdoc />
    public partial class AddVoucherToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VoucherCode",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VoucherDiscountPercent",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VoucherExpires",
                table: "Orders",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VoucherCode",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "VoucherDiscountPercent",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "VoucherExpires",
                table: "Orders");
        }
    }
}
