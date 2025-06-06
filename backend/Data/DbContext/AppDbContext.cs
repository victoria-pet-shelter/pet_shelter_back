using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<Users>().ToTable("Users");
        modelBuilder.Entity<Roles>().ToTable("Roles");
        modelBuilder.Entity<Shelters>().ToTable("Shelters");
        modelBuilder.Entity<Species>().ToTable("Species");
        modelBuilder.Entity<Breeds>().ToTable("Breeds");
        modelBuilder.Entity<Genders>().ToTable("Genders");
        modelBuilder.Entity<Pets>().ToTable("Pets");
        modelBuilder.Entity<AdoptionStatuses>().ToTable("AdoptionStatuses");
        modelBuilder.Entity<AdoptionRequests>().ToTable("AdoptionRequests");
        modelBuilder.Entity<Favorites>().ToTable("Favorites");
        modelBuilder.Entity<News>().ToTable("News");
        modelBuilder.Entity<Reviews>().ToTable("Reviews");

        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Species>().HasData(
            new Species { id = 1, name = "dog" },
            new Species { id = 2, name = "cat" },
            new Species { id = 3, name = "rabbit" },
            new Species { id = 4, name = "bird" },
            new Species { id = 5, name = "rodent" },
            new Species { id = 6, name = "reptile" },
            new Species { id = 7, name = "horse" },
            new Species { id = 8, name = "fish" },
            new Species { id = 9, name = "exotic" }
            // new Species { id = 999, name = "Unknown" }
        );

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

        // INDEXES
        // WHERE, JOIN, EXISTS, ANY, FindAsync = index need

        // Users
        modelBuilder.Entity<Users>()
            .HasIndex(u => u.email).IsUnique();

        modelBuilder.Entity<Users>()
            .HasIndex(u => u.name);

        // Shelters
        modelBuilder.Entity<Shelters>()
            .HasIndex(s => s.email).IsUnique();

        modelBuilder.Entity<Shelters>()
            .HasIndex(s => s.shelter_owner_id);

        // Pets
        modelBuilder.Entity<Pets>()
            .HasIndex(p => p.external_url).IsUnique();

        modelBuilder.Entity<Pets>()
            .HasIndex(p => p.shelter_id);

        modelBuilder.Entity<Pets>()
            .HasIndex(p => p.species_id);

        // Favorites
        modelBuilder.Entity<Favorites>()
            .HasIndex(f => new { f.user_id, f.pet_id });

        // Reviews
        modelBuilder.Entity<Reviews>()
            .HasIndex(r => new { r.user_id, r.shelter_id });

        // AdoptionRequests
        modelBuilder.Entity<AdoptionRequests>()
            .HasIndex(ar => new { ar.user_id, ar.pet_id });
    }
}
