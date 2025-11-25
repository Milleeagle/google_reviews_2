using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace google_reviews.Migrations
{
    /// <inheritdoc />
    public partial class AddIsCurrentCustomerToCompany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCurrentCustomer",
                table: "Companies",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCurrentCustomer",
                table: "Companies");
        }
    }
}
