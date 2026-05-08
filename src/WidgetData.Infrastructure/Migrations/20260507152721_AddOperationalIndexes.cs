using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WidgetData.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationalIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS "IX_WidgetExecutions_WidgetId";
                DROP INDEX IF EXISTS "IX_WidgetApiActivities_WidgetId";
                DROP INDEX IF EXISTS "IX_FormSubmissions_WidgetId";
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_WidgetExecutions_WidgetId_StartedAt"
                ON "WidgetExecutions" ("WidgetId", "StartedAt");
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_WidgetApiActivities_WidgetId_CalledAt"
                ON "WidgetApiActivities" ("WidgetId", "CalledAt");
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_FormSubmissions_WidgetId_SubmittedAt"
                ON "FormSubmissions" ("WidgetId", "SubmittedAt");
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_AuditLogs_Timestamp_Action"
                ON "AuditLogs" ("Timestamp", "Action");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS "IX_WidgetExecutions_WidgetId_StartedAt";
                DROP INDEX IF EXISTS "IX_WidgetApiActivities_WidgetId_CalledAt";
                DROP INDEX IF EXISTS "IX_FormSubmissions_WidgetId_SubmittedAt";
                DROP INDEX IF EXISTS "IX_AuditLogs_Timestamp_Action";
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_WidgetExecutions_WidgetId"
                ON "WidgetExecutions" ("WidgetId");
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_WidgetApiActivities_WidgetId"
                ON "WidgetApiActivities" ("WidgetId");
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_FormSubmissions_WidgetId"
                ON "FormSubmissions" ("WidgetId");
                """);
        }
    }
}
