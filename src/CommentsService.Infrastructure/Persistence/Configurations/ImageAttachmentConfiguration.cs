using CommentsService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace CommentsService.Infrastructure.Persistence.Configurations;

public sealed class ImageAttachmentConfiguration : IEntityTypeConfiguration<ImageAttachment>
{
    public void Configure(EntityTypeBuilder<ImageAttachment> image)
    {
        image.ToTable("ImageAttachments", table =>
        {
            table.HasCheckConstraint("CK_Images_Dimensions", "\"Width\" BETWEEN 1 AND 320 AND \"Height\" BETWEEN 1 AND 240");
            table.HasCheckConstraint("CK_Images_Data", "octet_length(\"Data\") BETWEEN 1 AND 524288");
        });
        image.HasKey(x => x.Id); image.Property(x => x.Id).ValueGeneratedNever();
        image.Property(x => x.FileName).HasMaxLength(128).IsRequired();
        image.Property(x => x.Data).HasField("_data").UsePropertyAccessMode(PropertyAccessMode.Field).HasColumnType("bytea").IsRequired();
        image.HasOne(x => x.Comment).WithOne(x => x.Image).HasForeignKey<ImageAttachment>(x => x.CommentId).OnDelete(DeleteBehavior.Cascade);
    }
}
