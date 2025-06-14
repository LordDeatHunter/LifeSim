using System.Globalization;
using LifeSim.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LifeSim.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> opts) : DbContext(opts)
{
    public DbSet<Balance> Balances { get; set; }
    public DbSet<Bet> Bets { get; set; }

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
    }
}