using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zeron.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddManagedPackageVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ManagedPackageVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PackageId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VersionNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ChangeKind = table.Column<string>(type: "TEXT", nullable: false),
                    ActorUsername = table.Column<string>(type: "TEXT", nullable: true),
                    RestoredFromVersion = table.Column<int>(type: "INTEGER", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Urlx86 = table.Column<string>(type: "TEXT", nullable: true),
                    Urlx64 = table.Column<string>(type: "TEXT", nullable: true),
                    CmdInstallx86 = table.Column<string>(type: "TEXT", nullable: true),
                    CmdInstallx64 = table.Column<string>(type: "TEXT", nullable: true),
                    CmdUnInstallx86 = table.Column<string>(type: "TEXT", nullable: true),
                    CmdUnInstallx64 = table.Column<string>(type: "TEXT", nullable: true),
                    ScriptInstallBefore = table.Column<string>(type: "TEXT", nullable: true),
                    ScriptInstallAfter = table.Column<string>(type: "TEXT", nullable: true),
                    ScriptUnInstallBefore = table.Column<string>(type: "TEXT", nullable: true),
                    ScriptUnInstallAfter = table.Column<string>(type: "TEXT", nullable: true),
                    Sha256x86 = table.Column<string>(type: "TEXT", nullable: true),
                    Sha256x64 = table.Column<string>(type: "TEXT", nullable: true),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManagedPackageVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManagedPackageVersions_ManagedPackages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "ManagedPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ManagedPackageVersions_PackageId_CreatedAt",
                table: "ManagedPackageVersions",
                columns: new[] { "PackageId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ManagedPackageVersions_PackageId_VersionNumber",
                table: "ManagedPackageVersions",
                columns: new[] { "PackageId", "VersionNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManagedPackageVersions");
        }
    }
}
