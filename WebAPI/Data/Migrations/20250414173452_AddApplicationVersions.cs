using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AyanamisTower.WebAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VersionNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ReleaseDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReleaseNotes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    OriginalFileName = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    StoredFileName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    StoredPath = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    Checksum = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    IsLatest = table.Column<bool>(type: "INTEGER", nullable: false),
                    Platform = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Architecture = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    RecordCreatedTimestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UploadedByUserId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationVersions_AspNetUsers_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

#pragma warning disable IDE0300 // Simplify collection initialization
#pragma warning disable CA1861 // Avoid constant arrays as arguments
            migrationBuilder.CreateIndex(
                name: "IX_ApplicationVersions_IsLatest_Platform_Architecture",
                table: "ApplicationVersions",
                columns: new[] { "IsLatest", "Platform", "Architecture" },
                filter: "[IsLatest] = 1");
#pragma warning restore CA1861 // Avoid constant arrays as arguments
#pragma warning restore IDE0300 // Simplify collection initialization

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationVersions_StoredFileName",
                table: "ApplicationVersions",
                column: "StoredFileName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationVersions_UploadedByUserId",
                table: "ApplicationVersions",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationVersions_VersionNumber",
                table: "ApplicationVersions",
                column: "VersionNumber");

#pragma warning disable CA1861 // Avoid constant arrays as arguments
            migrationBuilder.CreateIndex(
                name: "IX_ApplicationVersions_VersionNumber_Platform_Architecture",
                table: "ApplicationVersions",
                columns: new[] { "VersionNumber", "Platform", "Architecture" },
                unique: true);
#pragma warning restore CA1861 // Avoid constant arrays as arguments
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationVersions");
        }
    }
}
