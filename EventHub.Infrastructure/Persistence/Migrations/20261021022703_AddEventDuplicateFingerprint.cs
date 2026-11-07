using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEventDuplicateFingerprint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DuplicateFingerprint",
                table: "Events",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            // Create the unique index after existing records
            // have been assigned valid fingerprints.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Events_DuplicateFingerprint",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "DuplicateFingerprint",
                table: "Events");
        }
    }
}
