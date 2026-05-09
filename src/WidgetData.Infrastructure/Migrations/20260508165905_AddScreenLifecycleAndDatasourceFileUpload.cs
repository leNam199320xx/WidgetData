using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WidgetData.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScreenLifecycleAndDatasourceFileUpload : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentVersion",
                table: "Pages",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "LifecycleState",
                table: "Pages",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                table: "Pages",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublishedBy",
                table: "Pages",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScreenType",
                table: "Pages",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FileContentType",
                table: "DataSources",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FileSizeBytes",
                table: "DataSources",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileStoragePath",
                table: "DataSources",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FileUploadedAt",
                table: "DataSources",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileUploadedBy",
                table: "DataSources",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalFileName",
                table: "DataSources",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoredFileName",
                table: "DataSources",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PageVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PageId = table.Column<int>(type: "INTEGER", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    VersionNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    SnapshotJson = table.Column<string>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PageVersions_Pages_PageId",
                        column: x => x.PageId,
                        principalTable: "Pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PageVersions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PageVersions_PageId_VersionNumber",
                table: "PageVersions",
                columns: new[] { "PageId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PageVersions_TenantId",
                table: "PageVersions",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PageVersions");

            migrationBuilder.DropColumn(
                name: "CurrentVersion",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "LifecycleState",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "PublishedBy",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "ScreenType",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "FileContentType",
                table: "DataSources");

            migrationBuilder.DropColumn(
                name: "FileSizeBytes",
                table: "DataSources");

            migrationBuilder.DropColumn(
                name: "FileStoragePath",
                table: "DataSources");

            migrationBuilder.DropColumn(
                name: "FileUploadedAt",
                table: "DataSources");

            migrationBuilder.DropColumn(
                name: "FileUploadedBy",
                table: "DataSources");

            migrationBuilder.DropColumn(
                name: "OriginalFileName",
                table: "DataSources");

            migrationBuilder.DropColumn(
                name: "StoredFileName",
                table: "DataSources");
        }
    }
}
