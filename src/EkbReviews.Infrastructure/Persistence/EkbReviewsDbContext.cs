using EkbReviews.Domain;
using Microsoft.EntityFrameworkCore;

namespace EkbReviews.Infrastructure.Persistence;

public sealed class EkbReviewsDbContext(DbContextOptions<EkbReviewsDbContext> options) : DbContext(options)
{
    public DbSet<OrganizationEntity> Organizations => Set<OrganizationEntity>();
    public DbSet<ReviewEntity> Reviews => Set<ReviewEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrganizationEntity>(entity =>
        {
            entity.ToTable("organizations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(200);
            entity.Property(x => x.Name).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Address).HasMaxLength(1000);
            entity.HasIndex(x => x.Name);
        });

        modelBuilder.Entity<ReviewEntity>(entity =>
        {
            entity.ToTable("reviews");
            entity.HasKey(x => x.Key);
            entity.Property(x => x.Key).HasMaxLength(500);
            entity.Property(x => x.Id).HasMaxLength(300).IsRequired();
            entity.Property(x => x.OrganizationId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.OrganizationName).HasMaxLength(500).IsRequired();
            entity.Property(x => x.OrganizationAddress).HasMaxLength(1000);
            entity.Property(x => x.AuthorName).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Text).HasMaxLength(20000).IsRequired();
            entity.Property(x => x.SourceUrl).HasMaxLength(2000);
            entity.Property(x => x.Source).HasConversion<string>().HasMaxLength(50);
            entity.HasIndex(x => new { x.Source, x.Id }).IsUnique();
            entity.HasIndex(x => x.PublishedAt);
            entity.HasIndex(x => x.OrganizationId);
            entity.HasMany(x => x.Replies)
                .WithOne()
                .HasForeignKey(x => x.ReviewKey)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReviewReplyEntity>(entity =>
        {
            entity.ToTable("review_replies");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AuthorName).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Text).HasMaxLength(20000).IsRequired();
        });
    }
}

public sealed class OrganizationEntity
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Address { get; set; }
}

public sealed class ReviewEntity
{
    public string Key { get; set; } = null!;
    public string Id { get; set; } = null!;
    public ReviewSource Source { get; set; }
    public string OrganizationId { get; set; } = null!;
    public string OrganizationName { get; set; } = null!;
    public string? OrganizationAddress { get; set; }
    public int Rating { get; set; }
    public string AuthorName { get; set; } = null!;
    public DateTimeOffset PublishedAt { get; set; }
    public string Text { get; set; } = null!;
    public string? SourceUrl { get; set; }
    public DateTimeOffset CollectedAt { get; set; }
    public List<ReviewReplyEntity> Replies { get; set; } = [];
}

public sealed class ReviewReplyEntity
{
    public long Id { get; set; }
    public string ReviewKey { get; set; } = null!;
    public string AuthorName { get; set; } = null!;
    public string Text { get; set; } = null!;
    public DateTimeOffset PublishedAt { get; set; }
}
