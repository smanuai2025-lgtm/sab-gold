using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MrGoldenBader.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReadAtToAlert : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ReadAt",
                table: "Alerts",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReadAt",
                table: "Alerts");
        }
    }
}
