
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventHub.Infrastructure.Migrations
{
    public partial class AddTicketCategoryAvailableQuantity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AvailableQuantity",
                table: "TicketCategories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Preserve existing data and initialize available quantities.
            migrationBuilder.Sql("""
                UPDATE [TicketCategories]
                SET [AvailableQuantity] = [Capacity];
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvailableQuantity",
                table: "TicketCategories");
        }
    }
}
