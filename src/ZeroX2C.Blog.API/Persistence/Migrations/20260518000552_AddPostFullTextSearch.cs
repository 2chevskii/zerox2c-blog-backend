using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZeroX2C.Blog.API.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPostFullTextSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Tags_Name_Description",
                table: "Tags",
                columns: new[] { "Name", "Description" })
                .Annotation("MySql:FullTextIndex", true);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_Title_Subtitle_Slug",
                table: "Posts",
                columns: new[] { "Title", "Subtitle", "Slug" })
                .Annotation("MySql:FullTextIndex", true);

            migrationBuilder.CreateIndex(
                name: "IX_PostMarkdownDrafts_PlainText",
                table: "PostMarkdownDrafts",
                column: "PlainText")
                .Annotation("MySql:FullTextIndex", true);

            migrationBuilder.CreateIndex(
                name: "IX_PostMarkdownDocuments_PlainText",
                table: "PostMarkdownDocuments",
                column: "PlainText")
                .Annotation("MySql:FullTextIndex", true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tags_Name_Description",
                table: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_Posts_Title_Subtitle_Slug",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_PostMarkdownDrafts_PlainText",
                table: "PostMarkdownDrafts");

            migrationBuilder.DropIndex(
                name: "IX_PostMarkdownDocuments_PlainText",
                table: "PostMarkdownDocuments");
        }
    }
}
