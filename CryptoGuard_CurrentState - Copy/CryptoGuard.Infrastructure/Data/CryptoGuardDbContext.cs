using CryptoGuard.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace CryptoGuard.Infrastructure.Data
{
    public class CryptoGuardDbContext : DbContext
    {
        public CryptoGuardDbContext(DbContextOptions<CryptoGuardDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Coin> Coins { get; set; }
        public DbSet<Portfolio> Portfolios { get; set; }
        public DbSet<PortfolioItem> PortfolioItems { get; set; }
        public DbSet<PriceAlert> PriceAlerts { get; set; }
        public DbSet<FeedPost> FeedPosts { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Like> Likes { get; set; }
        public DbSet<Follow> Follows { get; set; }
        public DbSet<TransactionHistory> TransactionHistories { get; set; }
        public DbSet<FavoriteCoin> FavoriteCoins { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configurations
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .Property(u => u.IsPortfolioPublic)
                .HasDefaultValue(true);

            // Portfolio configurations
            modelBuilder.Entity<Portfolio>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // PortfolioItem configurations
            modelBuilder.Entity<PortfolioItem>()
                .HasOne(pi => pi.Portfolio)
                .WithMany(p => p.Items)
                .HasForeignKey(pi => pi.PortfolioId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PortfolioItem>()
                .HasOne(pi => pi.Coin)
                .WithMany()
                .HasForeignKey(pi => pi.CoinId)
                .OnDelete(DeleteBehavior.Restrict);

            // TransactionHistory configurations
            modelBuilder.Entity<TransactionHistory>()
                .HasOne(th => th.Portfolio)
                .WithMany()
                .HasForeignKey(th => th.PortfolioId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TransactionHistory>()
                .HasOne(th => th.Coin)
                .WithMany()
                .HasForeignKey(th => th.CoinId)
                .OnDelete(DeleteBehavior.Restrict);

            // PriceAlert configurations
            modelBuilder.Entity<PriceAlert>()
                .HasOne(pa => pa.User)
                .WithMany()
                .HasForeignKey(pa => pa.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PriceAlert>()
                .HasOne(pa => pa.Coin)
                .WithMany()
                .HasForeignKey(pa => pa.CoinId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FeedPost>()
                .HasOne(fp => fp.User)
                .WithMany()
                .HasForeignKey(fp => fp.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FeedPost>()
                .HasOne(fp => fp.OriginalPost)
                .WithMany()
                .HasForeignKey(fp => fp.OriginalPostId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.FeedPost)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Like>()
                .HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Like>()
                .HasOne(l => l.FeedPost)
                .WithMany(p => p.Likes)
                .HasForeignKey(l => l.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Follow>()
                .HasOne(f => f.Follower)
                .WithMany()
                .HasForeignKey(f => f.FollowerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Follow>()
                .HasOne(f => f.Following)
                .WithMany()
                .HasForeignKey(f => f.FollowingId)
                .OnDelete(DeleteBehavior.Restrict);

            // FavoriteCoin configurations
            modelBuilder.Entity<FavoriteCoin>()
                .HasKey(f => f.Id);
            modelBuilder.Entity<FavoriteCoin>()
                .Property(f => f.CoinId)
                .IsRequired();
            modelBuilder.Entity<FavoriteCoin>()
                .Property(f => f.UserId)
                .IsRequired();

            // DECIMAL PRECISION AYARLARI
            modelBuilder.Entity<Coin>(entity =>
            {
                entity.Property(e => e.CurrentPrice).HasPrecision(38, 18);
                entity.Property(e => e.MarketCap).HasPrecision(38, 18);
                entity.Property(e => e.Price).HasPrecision(38, 18);
                entity.Property(e => e.PriceChangePercentage24h).HasPrecision(38, 18);
            });
            modelBuilder.Entity<Portfolio>(entity =>
            {
                entity.Property(e => e.TotalValue).HasPrecision(38, 18);
            });
            modelBuilder.Entity<PortfolioItem>(entity =>
            {
                entity.Property(e => e.Quantity).HasPrecision(38, 18);
                entity.Property(e => e.Amount).HasPrecision(38, 18);
                entity.Property(e => e.BuyPrice).HasPrecision(38, 18);
                entity.Property(e => e.CurrentValue).HasPrecision(38, 18);
            });
            modelBuilder.Entity<PriceAlert>(entity =>
            {
                entity.Property(e => e.TargetPrice).HasPrecision(38, 18);
            });
            modelBuilder.Entity<TransactionHistory>(entity =>
            {
                entity.Property(e => e.Quantity).HasPrecision(38, 18);
                entity.Property(e => e.Price).HasPrecision(38, 18);
                entity.Property(e => e.TotalAmount).HasPrecision(38, 18);
            });
        }
    }
} 