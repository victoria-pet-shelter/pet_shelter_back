using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Models; // Models.cs

public class AppDbContext : DbContext
{
    private readonly string _connectionString;

    public AppDbContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public DbSet<Users> Users { get; set; }
    public DbSet<Roles> Roles { get; set; }
    public DbSet<Shelters> Shelters { get; set; }
    public DbSet<Species> Species { get; set; }
    public DbSet<Breeds> Breeds { get; set; }
    public DbSet<Genders> Genders { get; set; }
    public DbSet<Pets> Pets { get; set; }
    public DbSet<AdoptionStatuses> AdoptionStatuses { get; set; }
    public DbSet<AdoptionRequests> AdoptionRequests { get; set; }
    public DbSet<Favorites> Favorites { get; set; }
    public DbSet<News> News { get; set; }
    public DbSet<Reviews> Reviews { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseNpgsql(_connectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Shelter -> User (Owner)
        modelBuilder.Entity<Shelters>()
            .HasOne(s => s.Owner)
            .WithMany(u => u.Shelters)
            .HasForeignKey(s => s.shelter_owner_id)
            .OnDelete(DeleteBehavior.Cascade);

        // Pet -> Shelter
        modelBuilder.Entity<Pets>()
            .HasOne(p => p.Shelter)
            .WithMany()
            .HasForeignKey(p => p.shelter_id)
            .OnDelete(DeleteBehavior.Cascade);

        // Pet -> Species
        modelBuilder.Entity<Pets>()
            .HasOne(p => p.Species)
            .WithMany()
            .HasForeignKey(p => p.species_id)
            .OnDelete(DeleteBehavior.Cascade);

        // Pet -> Breed
        modelBuilder.Entity<Pets>()
            .HasOne(p => p.Breed)
            .WithMany()
            .HasForeignKey(p => p.breed_id)
            .OnDelete(DeleteBehavior.Cascade);

        // Pet -> Gender
        modelBuilder.Entity<Pets>()
            .HasOne(p => p.Gender)
            .WithMany()
            .HasForeignKey(p => p.gender_id)
            .OnDelete(DeleteBehavior.Cascade);

        // Favorites -> User, Pet
        modelBuilder.Entity<Favorites>()
            .HasOne<Users>()
            .WithMany()
            .HasForeignKey(f => f.user_id)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Favorites>()
            .HasOne<Pets>()
            .WithMany()
            .HasForeignKey(f => f.pet_id)
            .OnDelete(DeleteBehavior.Cascade);

        // Reviews -> User, Shelter
        modelBuilder.Entity<Reviews>()
            .HasOne<Users>()
            .WithMany()
            .HasForeignKey(r => r.user_id)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Reviews>()
            .HasOne<Shelters>()
            .WithMany()
            .HasForeignKey(r => r.shelter_id)
            .OnDelete(DeleteBehavior.Cascade);

        // AdoptionRequests -> User, Pet
        modelBuilder.Entity<AdoptionRequests>()
            .HasOne<Users>()
            .WithMany()
            .HasForeignKey(ar => ar.user_id)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AdoptionRequests>()
            .HasOne<Pets>()
            .WithMany()
            .HasForeignKey(ar => ar.pet_id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
