using CommentsService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommentsService.Infrastructure.Persistence.Configurations;

public sealed class AuthorConfiguration : IEntityTypeConfiguration<Author>
{
    public void Configure(EntityTypeBuilder<Author> author)
    {
        author.ToTable("Authors", table =>
            table.HasCheckConstraint("CK_Authors_UserName", "\"UserName\" ~ '^[A-Za-z0-9]+$'"));
        author.HasKey(x => x.Id);
        author.Property(x => x.Id).ValueGeneratedNever();
        author.Property(x => x.UserName).HasMaxLength(Author.UserNameMaxLength).IsRequired();
        author.Property(x => x.Email).HasMaxLength(Author.EmailMaxLength).IsRequired();
        author.Property(x => x.HomePage).HasMaxLength(Author.HomePageMaxLength);
        author.Navigation(x => x.Comments).HasField("_comments").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
