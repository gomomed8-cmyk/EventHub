using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueEventFingerprintIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Events_DuplicateFingerprint",
                table: "Events",
                column: "DuplicateFingerprint",
                unique: true,
                filter: "[DuplicateFingerprint] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Events_DuplicateFingerprint",
                table: "Events");

            migrationBuilder.AlterColumn<string>(
                name: "DuplicateFingerprint",
                table: "Events",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_DuplicateFingerprint",
                table: "Events",
                column: "DuplicateFingerprint",
                unique: true);
        }
    }
}
