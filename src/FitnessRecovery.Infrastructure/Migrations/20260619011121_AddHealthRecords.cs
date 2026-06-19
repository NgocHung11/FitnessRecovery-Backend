using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitnessRecovery.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHealthRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "health_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SleepHours = table.Column<double>(type: "double precision", nullable: false),
                    SleepQuality = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RestingHeartRate = table.Column<int>(type: "integer", nullable: false),
                    AverageHeartRate = table.Column<int>(type: "integer", nullable: false),
                    Steps = table.Column<int>(type: "integer", nullable: false),
                    Weight = table.Column<double>(type: "double precision", nullable: false),
                    CaloriesBurned = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_health_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_health_records_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_health_records_UserId_RecordDate",
                table: "health_records",
                columns: new[] { "UserId", "RecordDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "health_records");
        }
    }
}
