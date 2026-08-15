using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Balance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurringExpensePaymentType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "RecurringExpenses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "RecurringExpensePayments",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "RecurringExpenses");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "RecurringExpensePayments");
        }
    }
}
