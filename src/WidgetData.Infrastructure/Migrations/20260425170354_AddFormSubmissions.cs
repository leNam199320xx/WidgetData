using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WidgetData.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFormSubmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FormSubmissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WidgetId = table.Column<int>(type: "INTEGER", nullable: false),
                    Data = table.Column<string>(type: "TEXT", nullable: false),
                    SubmittedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IpAddress = table.Column<string>(type: "TEXT", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormSubmissions_Widgets_WidgetId",
                        column: x => x.WidgetId,
                        principalTable: "Widgets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmissions_WidgetId",
                table: "FormSubmissions",
                column: "WidgetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FormSubmissions");
        }
    }
}
