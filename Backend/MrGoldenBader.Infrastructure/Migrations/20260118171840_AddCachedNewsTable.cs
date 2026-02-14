using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MrGoldenBader.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCachedNewsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CachedNews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SourceDomain = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ApiSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ArticleUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AuthorsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KeywordsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sentiment = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SentimentScore = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    GoldImpactDirection = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GoldImpactScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ImpactExplanation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FetchedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CachedNews", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CachedNews_ApiSource",
                table: "CachedNews",
                column: "ApiSource");

            migrationBuilder.CreateIndex(
                name: "IX_CachedNews_Category",
                table: "CachedNews",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_CachedNews_ExpiresAt",
                table: "CachedNews",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_CachedNews_PublishedAt",
                table: "CachedNews",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CachedNews_Title_SourceDomain",
                table: "CachedNews",
                columns: new[] { "Title", "SourceDomain" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CachedNews");
        }
    }
}
