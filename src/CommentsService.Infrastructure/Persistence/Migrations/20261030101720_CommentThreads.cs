using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommentsService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CommentThreads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentId",
                schema: "comments",
                table: "Comments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RootId",
                schema: "comments",
                table: "Comments",
                type: "uuid",
                nullable: true);

            // Existing records are roots; preserve their IDs, contents and timestamps.
            migrationBuilder.Sql("UPDATE comments.\"Comments\" SET \"RootId\" = \"Id\";");
            migrationBuilder.AlterColumn<Guid>(
                name: "RootId", schema: "comments", table: "Comments", type: "uuid",
                nullable: false, oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Comments_Id_RootId",
                schema: "comments",
                table: "Comments",
                columns: new[] { "Id", "RootId" });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ParentId_CreatedAt_Id",
                schema: "comments",
                table: "Comments",
                columns: new[] { "ParentId", "CreatedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ParentId_RootId",
                schema: "comments",
                table: "Comments",
                columns: new[] { "ParentId", "RootId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Comments_Thread",
                schema: "comments",
                table: "Comments",
                sql: "(\"ParentId\" IS NULL AND \"RootId\" = \"Id\") OR (\"ParentId\" IS NOT NULL AND \"ParentId\" <> \"Id\" AND \"RootId\" <> \"Id\")");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Comments_ParentId_RootId",
                schema: "comments",
                table: "Comments",
                columns: new[] { "ParentId", "RootId" },
                principalSchema: "comments",
                principalTable: "Comments",
                principalColumns: new[] { "Id", "RootId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Removing thread metadata is safe only while all records are roots.
            migrationBuilder.Sql("""
                DO $guard$ BEGIN
                    IF EXISTS (SELECT 1 FROM comments."Comments" WHERE "ParentId" IS NOT NULL) THEN
                        RAISE EXCEPTION 'Cannot downgrade while replies exist. Preserve them or restore a verified checkpoint.';
                    END IF;
                END $guard$;
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Comments_ParentId_RootId",
                schema: "comments",
                table: "Comments");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Comments_Id_RootId",
                schema: "comments",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_Comments_ParentId_CreatedAt_Id",
                schema: "comments",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_Comments_ParentId_RootId",
                schema: "comments",
                table: "Comments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Comments_Thread",
                schema: "comments",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "ParentId",
                schema: "comments",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "RootId",
                schema: "comments",
                table: "Comments");
        }
    }
}
