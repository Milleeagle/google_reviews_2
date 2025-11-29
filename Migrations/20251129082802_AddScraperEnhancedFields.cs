using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace google_reviews.Migrations
{
    /// <inheritdoc />
    public partial class AddScraperEnhancedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BusinessResponse",
                table: "Reviews",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "Reviews",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RelativeTime",
                table: "Reviews",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Companies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "OverallRating",
                table: "Companies",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Companies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "Companies",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BusinessResponse",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "RelativeTime",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "OverallRating",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "Companies");
        }
    }
}
