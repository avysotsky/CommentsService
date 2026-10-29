using CommentsService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommentsService.Infrastructure.Persistence.Configurations;

public sealed class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> comment)
    {
        comment.ToTable("Comments", table =>
            table.HasCheckConstraint("CK_Comments_Text", "length(btrim(\"Text\")) > 0"));
        comment.HasKey(x => x.Id);
        comment.Property(x => x.Id).ValueGeneratedNever();
        comment.Property(x => x.Text).HasMaxLength(Comment.TextMaxLength).IsRequired();
        comment.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        comment.Property(x => x.ClientIp).HasColumnType("inet").IsRequired();
        comment.Property(x => x.UserAgent).HasMaxLength(Comment.UserAgentMaxLength).IsRequired();
        comment.HasOne(x => x.Author).WithMany(x => x.Comments)
            .HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
        comment.HasIndex(x => new { x.CreatedAt, x.Id }).IsDescending(true, true);
    }
}
