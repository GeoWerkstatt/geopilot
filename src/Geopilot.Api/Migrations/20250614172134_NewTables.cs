using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Geopilot.Api.Migrations
{
    /// <inheritdoc />
    public partial class NewTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ValidationJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MandateId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValidationJobs_Mandates_MandateId",
                        column: x => x.MandateId,
                        principalTable: "Mandates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ValidationJobFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ValidationJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Location = table.Column<string>(type: "text", nullable: false),
                    StorageType = table.Column<int>(type: "integer", nullable: false),
                    FileStatus = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ValidationResult = table.Column<string>(type: "text", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationJobFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValidationJobFiles_ValidationJobs_ValidationJobId",
                        column: x => x.ValidationJobId,
                        principalTable: "ValidationJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ValidationJobLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ValidationJobFileId = table.Column<int>(type: "integer", nullable: false),
                    LogName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Location = table.Column<string>(type: "text", nullable: false),
                    StorageType = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationJobLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValidationJobLogs_ValidationJobFiles_ValidationJobFileId",
                        column: x => x.ValidationJobFileId,
                        principalTable: "ValidationJobFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ValidationJobFiles_ValidationJobId_FileStatus",
                table: "ValidationJobFiles",
                columns: new[] { "ValidationJobId", "FileStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_ValidationJobLogs_ValidationJobFileId_LogName",
                table: "ValidationJobLogs",
                columns: new[] { "ValidationJobFileId", "LogName" });

            migrationBuilder.CreateIndex(
                name: "IX_ValidationJobs_MandateId",
                table: "ValidationJobs",
                column: "MandateId");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationJobs_Status",
                table: "ValidationJobs",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ValidationJobLogs");

            migrationBuilder.DropTable(
                name: "ValidationJobFiles");

            migrationBuilder.DropTable(
                name: "ValidationJobs");
        }
    }
}
