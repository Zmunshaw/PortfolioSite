using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Pgvector;

#nullable disable

namespace SiteBackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class Proto01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "Websites",
                columns: table => new
                {
                    WebsiteID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Host = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Websites", x => x.WebsiteID);
                });

            migrationBuilder.CreateTable(
                name: "Pages",
                columns: table => new
                {
                    PageID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LastCrawlAttempt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastCrawled = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WebsiteID = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pages", x => x.PageID);
                    table.ForeignKey(
                        name: "FK_Pages_Websites_WebsiteID",
                        column: x => x.WebsiteID,
                        principalTable: "Websites",
                        principalColumn: "WebsiteID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sitemap",
                columns: table => new
                {
                    SitemapID = table.Column<int>(type: "integer", nullable: false),
                    Location = table.Column<string>(type: "text", nullable: false),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ParentSitemapId = table.Column<int>(type: "integer", nullable: true),
                    IsMapped = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sitemap", x => x.SitemapID);
                    table.ForeignKey(
                        name: "FK_Sitemap_Sitemap_ParentSitemapId",
                        column: x => x.ParentSitemapId,
                        principalTable: "Sitemap",
                        principalColumn: "SitemapID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sitemap_Websites_SitemapID",
                        column: x => x.SitemapID,
                        principalTable: "Websites",
                        principalColumn: "WebsiteID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Contents",
                columns: table => new
                {
                    ContentID = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Text = table.Column<string>(type: "text", maxLength: 26214400, nullable: true),
                    ContentHash = table.Column<string>(type: "text", nullable: true),
                    NeedsEmbedding = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contents", x => x.ContentID);
                    table.ForeignKey(
                        name: "FK_Contents_Pages_ContentID",
                        column: x => x.ContentID,
                        principalTable: "Pages",
                        principalColumn: "PageID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Urls",
                columns: table => new
                {
                    UrlID = table.Column<int>(type: "integer", nullable: false),
                    SitemapID = table.Column<int>(type: "integer", nullable: true),
                    Location = table.Column<string>(type: "text", nullable: false),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ChangeFrequency = table.Column<int>(type: "integer", nullable: true),
                    Priority = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Urls", x => x.UrlID);
                    table.ForeignKey(
                        name: "FK_Urls_Pages_UrlID",
                        column: x => x.UrlID,
                        principalTable: "Pages",
                        principalColumn: "PageID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Urls_Sitemap_SitemapID",
                        column: x => x.SitemapID,
                        principalTable: "Sitemap",
                        principalColumn: "SitemapID");
                });

            migrationBuilder.CreateTable(
                name: "TextEmbeddings",
                columns: table => new
                {
                    TextEmbeddingID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmbeddingHash = table.Column<string>(type: "text", nullable: true),
                    RawText = table.Column<string>(type: "text", nullable: true),
                    Embedding = table.Column<Vector>(type: "vector(768)", nullable: true),
                    ContentID = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TextEmbeddings", x => x.TextEmbeddingID);
                    table.ForeignKey(
                        name: "FK_TextEmbeddings_Contents_ContentID",
                        column: x => x.ContentID,
                        principalTable: "Contents",
                        principalColumn: "ContentID");
                });

            migrationBuilder.CreateTable(
                name: "MediaEntry",
                columns: table => new
                {
                    MediaEntryID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Location = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    UrlID = table.Column<int>(type: "integer", nullable: true),
                    Publication = table.Column<string>(type: "text", nullable: true),
                    PublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Language = table.Column<string>(type: "text", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: true),
                    ThumbnailLocation = table.Column<string>(type: "text", nullable: true),
                    VideoEntry_Title = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ContentLocation = table.Column<string>(type: "text", nullable: true),
                    PlayerLocation = table.Column<string>(type: "text", nullable: true),
                    Duration = table.Column<TimeSpan>(type: "interval", nullable: true),
                    Rating = table.Column<float>(type: "real", nullable: true),
                    ViewCount = table.Column<int>(type: "integer", nullable: true),
                    VideoEntry_PublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Restrictions = table.Column<string>(type: "text", nullable: true),
                    Platform = table.Column<string>(type: "text", nullable: true),
                    RequiresSubscription = table.Column<string>(type: "text", nullable: true),
                    Tag = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaEntry", x => x.MediaEntryID);
                    table.ForeignKey(
                        name: "FK_MediaEntry_Urls_UrlID",
                        column: x => x.UrlID,
                        principalTable: "Urls",
                        principalColumn: "UrlID");
                });

            migrationBuilder.CreateTable(
                name: "Words",
                columns: table => new
                {
                    WordID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Text = table.Column<string>(type: "text", nullable: false),
                    Embedding = table.Column<SparseVector>(type: "sparsevec(768)", nullable: true),
                    TextEmbeddingID = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Words", x => x.WordID);
                    table.ForeignKey(
                        name: "FK_Words_TextEmbeddings_TextEmbeddingID",
                        column: x => x.TextEmbeddingID,
                        principalTable: "TextEmbeddings",
                        principalColumn: "TextEmbeddingID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntry_UrlID",
                table: "MediaEntry",
                column: "UrlID");

            migrationBuilder.CreateIndex(
                name: "IX_Pages_WebsiteID",
                table: "Pages",
                column: "WebsiteID");

            migrationBuilder.CreateIndex(
                name: "IX_Sitemap_ParentSitemapId",
                table: "Sitemap",
                column: "ParentSitemapId");

            migrationBuilder.CreateIndex(
                name: "IX_TextEmbeddings_ContentID",
                table: "TextEmbeddings",
                column: "ContentID");

            migrationBuilder.CreateIndex(
                name: "IX_Urls_SitemapID",
                table: "Urls",
                column: "SitemapID");

            migrationBuilder.CreateIndex(
                name: "IX_Words_TextEmbeddingID",
                table: "Words",
                column: "TextEmbeddingID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MediaEntry");

            migrationBuilder.DropTable(
                name: "Words");

            migrationBuilder.DropTable(
                name: "Urls");

            migrationBuilder.DropTable(
                name: "TextEmbeddings");

            migrationBuilder.DropTable(
                name: "Sitemap");

            migrationBuilder.DropTable(
                name: "Contents");

            migrationBuilder.DropTable(
                name: "Pages");

            migrationBuilder.DropTable(
                name: "Websites");
        }
    }
}
