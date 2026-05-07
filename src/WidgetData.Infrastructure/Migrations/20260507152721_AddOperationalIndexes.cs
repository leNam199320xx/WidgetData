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
            migrationBuilder.DropIndex(
                name: "IX_WidgetExecutions_WidgetId",
                table: "WidgetExecutions");

            migrationBuilder.DropIndex(
                name: "IX_WidgetApiActivities_WidgetId",
                table: "WidgetApiActivities");

            migrationBuilder.DropIndex(
                name: "IX_FormSubmissions_WidgetId",
                table: "FormSubmissions");

            migrationBuilder.CreateIndex(
                name: "IX_WidgetExecutions_WidgetId_StartedAt",
                table: "WidgetExecutions",
                columns: new[] { "WidgetId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WidgetApiActivities_WidgetId_CalledAt",
                table: "WidgetApiActivities",
                columns: new[] { "WidgetId", "CalledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmissions_WidgetId_SubmittedAt",
                table: "FormSubmissions",
                columns: new[] { "WidgetId", "SubmittedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp_Action",
                table: "AuditLogs",
                columns: new[] { "Timestamp", "Action" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WidgetExecutions_WidgetId_StartedAt",
                table: "WidgetExecutions");

            migrationBuilder.DropIndex(
                name: "IX_WidgetApiActivities_WidgetId_CalledAt",
                table: "WidgetApiActivities");

            migrationBuilder.DropIndex(
                name: "IX_FormSubmissions_WidgetId_SubmittedAt",
                table: "FormSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Timestamp_Action",
                table: "AuditLogs");

            migrationBuilder.CreateIndex(
                name: "IX_WidgetExecutions_WidgetId",
                table: "WidgetExecutions",
                column: "WidgetId");

            migrationBuilder.CreateIndex(
                name: "IX_WidgetApiActivities_WidgetId",
                table: "WidgetApiActivities",
                column: "WidgetId");

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmissions_WidgetId",
                table: "FormSubmissions",
                column: "WidgetId");
        }
    }
}
