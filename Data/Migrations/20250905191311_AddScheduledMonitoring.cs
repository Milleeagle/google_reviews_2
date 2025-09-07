using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace google_reviews.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledMonitoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScheduledReviewMonitors",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmailAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScheduleType = table.Column<int>(type: "int", nullable: false),
                    ScheduleTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: true),
                    DayOfMonth = table.Column<int>(type: "int", nullable: true),
                    MaxRating = table.Column<int>(type: "int", nullable: false),
                    ReviewPeriodDays = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IncludeAllCompanies = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastRunAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NextRunAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledReviewMonitors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledMonitorCompanies",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ScheduledReviewMonitorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CompanyId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledMonitorCompanies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduledMonitorCompanies_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScheduledMonitorCompanies_ScheduledReviewMonitors_ScheduledReviewMonitorId",
                        column: x => x.ScheduledReviewMonitorId,
                        principalTable: "ScheduledReviewMonitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledMonitorExecutions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ScheduledReviewMonitorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ExecutedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompaniesChecked = table.Column<int>(type: "int", nullable: false),
                    CompaniesWithIssues = table.Column<int>(type: "int", nullable: false),
                    TotalBadReviews = table.Column<int>(type: "int", nullable: false),
                    EmailSent = table.Column<bool>(type: "bit", nullable: false),
                    EmailError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledMonitorExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduledMonitorExecutions_ScheduledReviewMonitors_ScheduledReviewMonitorId",
                        column: x => x.ScheduledReviewMonitorId,
                        principalTable: "ScheduledReviewMonitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledMonitorCompanies_CompanyId",
                table: "ScheduledMonitorCompanies",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledMonitorCompanies_ScheduledReviewMonitorId",
                table: "ScheduledMonitorCompanies",
                column: "ScheduledReviewMonitorId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledMonitorExecutions_ExecutedAt",
                table: "ScheduledMonitorExecutions",
                column: "ExecutedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledMonitorExecutions_ScheduledReviewMonitorId",
                table: "ScheduledMonitorExecutions",
                column: "ScheduledReviewMonitorId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledReviewMonitors_IsActive",
                table: "ScheduledReviewMonitors",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledReviewMonitors_NextRunAt",
                table: "ScheduledReviewMonitors",
                column: "NextRunAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduledMonitorCompanies");

            migrationBuilder.DropTable(
                name: "ScheduledMonitorExecutions");

            migrationBuilder.DropTable(
                name: "ScheduledReviewMonitors");
        }
    }
}
