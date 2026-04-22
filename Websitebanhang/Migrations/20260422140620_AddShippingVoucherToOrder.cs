using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Websitebanhang.Migrations
{
    /// <inheritdoc />
    public partial class AddShippingVoucherToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShippingVoucherCode",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShippingVoucherDiscountPercent",
                table: "Orders",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShippingVoucherCode",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingVoucherDiscountPercent",
                table: "Orders");
        }
    }
}
