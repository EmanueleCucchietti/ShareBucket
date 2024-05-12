using DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Context;

public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {

    }

    public DbSet<UserModel> Users { get; set; }
    public DbSet<MemoryAreaModel> MemoryAreas { get; set; }
    public DbSet<MetadataModel> Metadatas { get; set; }
    public DbSet<ModifierModel> Modifiers { get; set; }
    public DbSet<TierModel> Tiers { get; set; }
    public DbSet<TransactionModel> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // N:M Relationship between User and MemoryArea
        modelBuilder.Entity<UserModel>().
            HasMany(u => u.MemoryAreasPartecipated).
            WithMany(m => m.Users).
            UsingEntity<PartecipateModel>();

        // N:M Relatioship between User and User through Friendship
        modelBuilder.Entity<FriendshipModel>()
            .HasKey(f => new { f.UserId, f.FriendId });

        modelBuilder.Entity<FriendshipModel>()
            .HasOne(f => f.User)
            .WithMany(u => u.Friendships)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Restrict);


    }
}
