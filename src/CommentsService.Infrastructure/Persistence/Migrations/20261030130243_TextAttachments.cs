using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommentsService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TextAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TextAttachments",
                schema: "comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CommentId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SizeBytes = table.Column<int>(type: "integer", nullable: false),
                    Data = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TextAttachments", x => x.Id);
                    table.CheckConstraint("CK_TextAttachments_Size", "octet_length(\"Data\") BETWEEN 1 AND 102400 AND \"SizeBytes\" = octet_length(\"Data\")");
                    table.ForeignKey(
                        name: "FK_TextAttachments_Comments_CommentId",
                        column: x => x.CommentId,
                        principalSchema: "comments",
                        principalTable: "Comments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TextAttachments_CommentId",
                schema: "comments",
                table: "TextAttachments",
                column: "CommentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $guard$ BEGIN
                    IF EXISTS (SELECT 1 FROM comments."TextAttachments") THEN
                        RAISE EXCEPTION 'Cannot downgrade while TXT files exist. Preserve them or restore a verified checkpoint.';
                    END IF;
                END $guard$;
                """);
            migrationBuilder.DropTable(
                name: "TextAttachments",
                schema: "comments");
        }
    }
}
