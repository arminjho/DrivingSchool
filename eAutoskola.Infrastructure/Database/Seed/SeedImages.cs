namespace eAutoskola.Infrastructure.Database.Seed;

/// <summary>
/// Copies a placeholder image from the build output's SeedAssets (see
/// eAutoskola.Infrastructure.csproj) into ./uploads/seed - the same folder FileStorageService
/// serves real uploads from - and returns the path exactly as that service stores it
/// (upute.md §3.1: "Seed mora sadržavati i slike ako domena koristi slike."). Every record gets
/// its own copy, so replacing or deleting one record's image never breaks another's.
/// </summary>
internal static class SeedImages
{
    public static string Copy(string fileName)
    {
        var source = Path.Combine(AppContext.BaseDirectory, "SeedAssets", fileName);
        var uploadsDir = Path.Combine("uploads", "seed");
        Directory.CreateDirectory(uploadsDir);

        var destination = Path.Combine(uploadsDir, fileName);
        File.Copy(source, destination, overwrite: true);

        return destination.Replace('\\', '/');
    }
}
