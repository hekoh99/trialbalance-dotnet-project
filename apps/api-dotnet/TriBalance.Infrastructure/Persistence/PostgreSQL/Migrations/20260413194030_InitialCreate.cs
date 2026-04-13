using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TriBalance.Infrastructure.Persistence.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "engagements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    client_name = table.Column<string>(type: "text", nullable: false),
                    fiscal_year_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_engagements", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "validation_jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    engagement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trial_balance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "Queued"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_validation_jobs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "trial_balances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    engagement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "text", nullable: false),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    total_debits = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_credits = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trial_balances", x => x.id);
                    table.ForeignKey(
                        name: "FK_trial_balances_engagements_engagement_id",
                        column: x => x.engagement_id,
                        principalTable: "engagements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gl_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    trial_balance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    engagement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_code = table.Column<string>(type: "text", nullable: false),
                    account_name = table.Column<string>(type: "text", nullable: false),
                    debit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    credit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gl_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_gl_entries_trial_balances_trial_balance_id",
                        column: x => x.trial_balance_id,
                        principalTable: "trial_balances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_gl_entries_engagement",
                table: "gl_entries",
                column: "engagement_id");

            migrationBuilder.CreateIndex(
                name: "idx_gl_entries_trial_balance",
                table: "gl_entries",
                column: "trial_balance_id");

            migrationBuilder.CreateIndex(
                name: "IX_trial_balances_engagement_id",
                table: "trial_balances",
                column: "engagement_id");

            migrationBuilder.CreateIndex(
                name: "idx_validation_jobs_engagement",
                table: "validation_jobs",
                column: "engagement_id");

            migrationBuilder.CreateIndex(
                name: "idx_validation_jobs_status",
                table: "validation_jobs",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gl_entries");

            migrationBuilder.DropTable(
                name: "validation_jobs");

            migrationBuilder.DropTable(
                name: "trial_balances");

            migrationBuilder.DropTable(
                name: "engagements");
        }
    }
}
