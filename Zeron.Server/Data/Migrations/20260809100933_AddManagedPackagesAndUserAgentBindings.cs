using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zeron.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddManagedPackagesAndUserAgentBindings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ManagedPackages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
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
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManagedPackages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserAgentBindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AgentKey = table.Column<string>(type: "TEXT", nullable: false),
                    BoundAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAgentBindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAgentBindings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ManagedPackages_IsEnabled",
                table: "ManagedPackages",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_ManagedPackages_Name",
                table: "ManagedPackages",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAgentBindings_AgentKey",
                table: "UserAgentBindings",
                column: "AgentKey");

            migrationBuilder.CreateIndex(
                name: "IX_UserAgentBindings_UserId_AgentKey",
                table: "UserAgentBindings",
                columns: new[] { "UserId", "AgentKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManagedPackages");

            migrationBuilder.DropTable(
                name: "UserAgentBindings");
        }
    }
}
