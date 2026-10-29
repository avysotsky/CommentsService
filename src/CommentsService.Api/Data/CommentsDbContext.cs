using CommentsService.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommentsService.Api.Data;

public sealed class CommentsDbContext(DbContextOptions<CommentsDbContext> options) : DbContext(options)
{
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Comment> Comments => Set<Comment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("comments");
        var author = modelBuilder.Entity<Author>();
        author.ToTable("Authors", table =>
            table.HasCheckConstraint("CK_Authors_UserName", "\"UserName\" ~ '^[A-Za-z0-9]+$'"));
        author.HasKey(x => x.Id);
        author.Property(x => x.Id).ValueGeneratedNever();
        author.Property(x => x.UserName).HasMaxLength(100).IsRequired();
        author.Property(x => x.Email).HasMaxLength(254).IsRequired();
        author.Property(x => x.HomePage).HasMaxLength(2048);

        var comment = modelBuilder.Entity<Comment>();
        comment.ToTable("Comments", table =>
            table.HasCheckConstraint("CK_Comments_Text", "length(btrim(\"Text\")) > 0"));
        comment.HasKey(x => x.Id);
        comment.Property(x => x.Id).ValueGeneratedNever();
        comment.Property(x => x.Text).HasMaxLength(65536).IsRequired();
        comment.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        comment.Property(x => x.ClientIp).HasColumnType("inet").IsRequired();
        comment.Property(x => x.UserAgent).HasMaxLength(2048).IsRequired();
        comment.HasOne(x => x.Author).WithMany(x => x.Comments)
            .HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
        comment.HasIndex(x => new { x.CreatedAt, x.Id }).IsDescending(true, true);
    }
}
