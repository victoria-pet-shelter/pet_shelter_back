using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Models; // Models.cs

public class AppDbContext : DbContext
{
    private readonly IConfiguration _configuration;

    public AppDbContext(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public DbSet<User> Users { get; set; }
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
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        options.UseNpgsql(connectionString);
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
            .HasForeignKey(p => p.species_id);

        // Pet -> Breed
        modelBuilder.Entity<Pets>()
            .HasOne(p => p.Breed)
            .WithMany()
            .HasForeignKey(p => p.breed_id);

        // Pet -> Gender
        modelBuilder.Entity<Pets>()
            .HasOne(p => p.Gender)
            .WithMany()
            .HasForeignKey(p => p.gender_id);
    }
}
