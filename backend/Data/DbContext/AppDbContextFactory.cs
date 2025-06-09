using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;
using System.IO;
using DotNetEnv;
using System;

// Provides a way to create AppDbContext during design-time (e.g., for migrations)
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var root = Directory.GetParent(Directory.GetCurrentDirectory())!.FullName;
        Env.Load(Path.Combine(root, ".env"));

        var host = Environment.GetEnvironmentVariable("DB_HOST");
        var db = Environment.GetEnvironmentVariable("DB_NAME");
        var user = Environment.GetEnvironmentVariable("DB_USER");
        var pass = Environment.GetEnvironmentVariable("DB_PASSWORD");

        var connectionString = $"Host={host};Database={db};Username={user};Password={pass}";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        // Return new instance of AppDbContext with configured options
        return new AppDbContext(optionsBuilder.Options);
    }
}
