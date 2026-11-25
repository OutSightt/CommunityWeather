using CommunityWeather.Models;
using Microsoft.EntityFrameworkCore;

namespace CommunityWeather.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Advisory> Advisories => Set<Advisory>();
    public DbSet<EvacuationCenter> EvacuationCenters => Set<EvacuationCenter>();
    public DbSet<PreparednessGuide> PreparednessGuides => Set<PreparednessGuide>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<Advisory>().HasIndex(a => new { a.Status, a.PublishedAt });
        modelBuilder.Entity<PreparednessGuide>().HasIndex(g => g.Hazard);
    }
}
