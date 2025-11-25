using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace google_reviews.Migrations
{
    /// <inheritdoc />
    public partial class _20250924120000_AddEmailAddressToCompany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailAddress",
                table: "Companies",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailAddress",
                table: "Companies");
        }
    }
}
