using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitnessRecovery.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecoveryAnalyses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "recovery_analyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnalysisDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RecoveryScore = table.Column<int>(type: "integer", nullable: false),
                    RecoveryStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SleepScore = table.Column<int>(type: "integer", nullable: false),
                    HeartRateScore = table.Column<int>(type: "integer", nullable: false),
                    WorkoutLoadScore = table.Column<int>(type: "integer", nullable: false),
                    ActivityScore = table.Column<int>(type: "integer", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recovery_analyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_recovery_analyses_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_recovery_analyses_UserId_AnalysisDate",
                table: "recovery_analyses",
                columns: new[] { "UserId", "AnalysisDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "recovery_analyses");
        }
    }
}
