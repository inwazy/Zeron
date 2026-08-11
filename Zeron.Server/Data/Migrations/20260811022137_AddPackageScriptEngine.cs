using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zeron.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPackageScriptEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ScriptEngine",
                table: "ManagedPackageVersions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScriptEngine",
                table: "ManagedPackages",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScriptEngine",
                table: "ManagedPackageVersions");

            migrationBuilder.DropColumn(
                name: "ScriptEngine",
                table: "ManagedPackages");
        }
    }
}
