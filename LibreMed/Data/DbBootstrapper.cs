// LibreMed/Data/DbBootstrapper.cs
using System;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace LibreMed.Data;

public static class DbBootstrapper
{
    public static void EnsureCreated()
    {
        var dbPath = GetDatabasePath();
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        using var db = new AppDbContext(options);

        db.Database.Migrate();        // apply migrations (creates DB + tables if needed)
        DbSeeder.SeedIfEmpty(db);     // seed demo data once
    }

    public static string GetDatabasePath()
    {
        // Cross-platform-ish location:
        // Linux: ~/.local/share/LibreMed/libremed.db
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(baseDir, "LibreMed", "libremed.db");
    }
}