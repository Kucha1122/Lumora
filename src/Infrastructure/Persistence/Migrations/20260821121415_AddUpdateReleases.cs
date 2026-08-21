using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lumora.Server.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUpdateReleases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UpdateReleases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    VersionCode = table.Column<int>(type: "int", nullable: false),
                    BlobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ReleaseNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpdateReleases", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UpdateReleases_VersionCode",
                table: "UpdateReleases",
                column: "VersionCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UpdateReleases");
        }
    }
}
