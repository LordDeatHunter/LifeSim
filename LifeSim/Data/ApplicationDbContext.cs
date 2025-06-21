using System.Globalization;
using LifeSim.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LifeSim.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> opts) : DbContext(opts)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Bet> Bets { get; set; }

    public DbSet<FoodEntity> Foods { get; set; }
    public DbSet<AnimalEntity> Animals { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Bet>()
            .Property(b => b.ExpiresAt)
            .HasConversion(
                dto => dto.ToString("O"),
                str => DateTimeOffset.Parse(
                    str,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind | DateTimeStyles.AssumeUniversal
                )
            )
            .HasColumnType("TEXT");

        modelBuilder.Entity<AnimalEntity>(b =>
        {
            b.Property(e => e.Health)
                .IsRequired()
                .HasDefaultValue(20F);

            b.Property(e => e.MaxHealth)
                .IsRequired()
                .HasDefaultValue(20F);
            
            b.Property(e => e.DefaultSpeed)
                .IsRequired()
                .HasDefaultValue(16F);
        });
    }
}