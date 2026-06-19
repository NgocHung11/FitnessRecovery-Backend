using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitnessRecovery.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecommendations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "recommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecoveryAnalysisId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecommendationType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_recommendations_recovery_analyses_RecoveryAnalysisId",
                        column: x => x.RecoveryAnalysisId,
                        principalTable: "recovery_analyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_recommendations_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_recommendations_RecoveryAnalysisId",
                table: "recommendations",
                column: "RecoveryAnalysisId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_recommendations_UserId",
                table: "recommendations",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "recommendations");
        }
    }
}
