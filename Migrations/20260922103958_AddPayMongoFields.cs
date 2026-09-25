using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmFlow.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPayMongoFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PayMongoChannel",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayMongoCheckoutSessionId",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayMongoPaymentIntentId",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PayMongoChannel",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PayMongoCheckoutSessionId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PayMongoPaymentIntentId",
                table: "Payments");
        }
    }
}
