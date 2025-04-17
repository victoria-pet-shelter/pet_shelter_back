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
}
