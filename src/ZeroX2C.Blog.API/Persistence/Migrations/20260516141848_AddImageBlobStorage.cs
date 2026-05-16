using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZeroX2C.Blog.API.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddImageBlobStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "Content",
                table: "Images",
                type: "longblob",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "Images",
                type: "varchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "OriginalFileName",
                table: "Images",
                type: "varchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Purpose",
                table: "Images",
                type: "varchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<long>(
                name: "SizeBytes",
                table: "Images",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_BannerImageId",
                table: "Posts",
                column: "BannerImageId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_CoverImageId",
                table: "Posts",
                column: "CoverImageId");

            migrationBuilder.CreateIndex(
                name: "IX_Images_Purpose",
                table: "Images",
                column: "Purpose");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Images_BannerImageId",
                table: "Posts",
                column: "BannerImageId",
                principalTable: "Images",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Images_CoverImageId",
                table: "Posts",
                column: "CoverImageId",
                principalTable: "Images",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Images_BannerImageId",
                table: "Posts");

            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Images_CoverImageId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_BannerImageId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_CoverImageId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Images_Purpose",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "Content",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "OriginalFileName",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "Purpose",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "SizeBytes",
                table: "Images");
        }
    }
}
