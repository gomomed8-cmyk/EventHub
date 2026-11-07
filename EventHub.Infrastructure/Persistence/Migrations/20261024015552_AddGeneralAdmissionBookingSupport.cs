using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGeneralAdmissionBookingSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AvailableQuantity",
                table: "Events",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Initialize existing events from their configured capacity.
            migrationBuilder.Sql("""
                UPDATE [Events]
                SET [AvailableQuantity] = [Capacity];
                """);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Events",
                type: "rowversion",
                rowVersion: true,
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "AvailableQuantity",
                table: "Events");
        }
    }
}