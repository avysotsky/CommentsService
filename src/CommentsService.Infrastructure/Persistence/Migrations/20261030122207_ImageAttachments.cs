using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommentsService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImageAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImageAttachments",
                schema: "comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CommentId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: false),
                    Height = table.Column<int>(type: "integer", nullable: false),
                    Data = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageAttachments", x => x.Id);
                    table.CheckConstraint("CK_Images_Data", "octet_length(\"Data\") BETWEEN 1 AND 524288");
                    table.CheckConstraint("CK_Images_Dimensions", "\"Width\" BETWEEN 1 AND 320 AND \"Height\" BETWEEN 1 AND 240");
                    table.ForeignKey(
                        name: "FK_ImageAttachments_Comments_CommentId",
                        column: x => x.CommentId,
                        principalSchema: "comments",
                        principalTable: "Comments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImageAttachments_CommentId",
                schema: "comments",
                table: "ImageAttachments",
                column: "CommentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $guard$ BEGIN
                    IF EXISTS (SELECT 1 FROM comments."ImageAttachments") THEN
                        RAISE EXCEPTION 'Cannot downgrade while images exist. Preserve them or restore a verified checkpoint.';
                    END IF;
                END $guard$;
                """);
            migrationBuilder.DropTable(
                name: "ImageAttachments",
                schema: "comments");
        }
    }
}
