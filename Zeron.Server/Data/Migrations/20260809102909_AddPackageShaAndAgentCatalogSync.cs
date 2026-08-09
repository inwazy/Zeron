using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zeron.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPackageShaAndAgentCatalogSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Sha256x64",
                table: "ManagedPackages",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sha256x86",
                table: "ManagedPackages",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCatalogSyncAt",
                table: "Agents",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Sha256x64",
                table: "ManagedPackages");

            migrationBuilder.DropColumn(
                name: "Sha256x86",
                table: "ManagedPackages");

            migrationBuilder.DropColumn(
                name: "LastCatalogSyncAt",
                table: "Agents");
        }
    }
}
