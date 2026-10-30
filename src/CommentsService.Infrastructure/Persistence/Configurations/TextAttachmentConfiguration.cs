using CommentsService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace CommentsService.Infrastructure.Persistence.Configurations;
public sealed class TextAttachmentConfiguration : IEntityTypeConfiguration<TextAttachment>
{
    public void Configure(EntityTypeBuilder<TextAttachment> file)
    {
        file.ToTable("TextAttachments", table => table.HasCheckConstraint("CK_TextAttachments_Size", "octet_length(\"Data\") BETWEEN 1 AND 102400 AND \"SizeBytes\" = octet_length(\"Data\")"));
        file.HasKey(x => x.Id); file.Property(x => x.Id).ValueGeneratedNever();
        file.Property(x => x.FileName).HasMaxLength(128).IsRequired();
        file.Property(x => x.Data).HasField("_data").UsePropertyAccessMode(PropertyAccessMode.Field).HasColumnType("bytea").IsRequired();
        file.HasOne(x => x.Comment).WithOne(x => x.TextFile).HasForeignKey<TextAttachment>(x => x.CommentId).OnDelete(DeleteBehavior.Cascade);
    }
}
