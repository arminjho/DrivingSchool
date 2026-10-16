using eAutoskola.Domain.Resources;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Domain.Files;

namespace eAutoskola.Infrastructure.Files;

/// <summary>
/// Local disk storage under ./uploads. Validates MIME type AND magic bytes (never just the
/// extension - upute.md §5), enforces a size limit, and generates a safe GUID-based file name
/// so the original (possibly attacker-controlled) file name is never used on disk.
/// </summary>
public class FileStorageService : IFileStorageService
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
    private const string UploadsRoot = "uploads";

    private static readonly Dictionary<string, byte[]> AllowedMagicBytes = new()
    {
        ["image/jpeg"] = [0xFF, 0xD8, 0xFF],
        ["image/png"] = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
    };

    public FileStorageService()
    {
        Directory.CreateDirectory(UploadsRoot);
    }

    public async Task<string> SaveImageAsync(Stream content, string originalFileName, string contentType, CancellationToken cancellationToken)
    {
        if (content.Length > MaxFileSizeBytes)
        {
            throw new ValidationException("file", string.Format(Messages.FileTooLarge, MaxFileSizeBytes / 1024 / 1024));
        }

        var header = new byte[12];
        var read = await content.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
        content.Position = 0;

        var isWebp = read >= 12 && IsWebp(header);
        var matchesDeclaredType = AllowedMagicBytes.TryGetValue(contentType, out var expectedMagic)
            && header.AsSpan(0, expectedMagic.Length).SequenceEqual(expectedMagic);

        if (!matchesDeclaredType && !(contentType == "image/webp" && isWebp))
        {
            throw new ValidationException("file", Messages.InvalidFileType);
        }

        var extension = contentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => throw new ValidationException("file", Messages.InvalidFileType),
        };

        var safeFileName = $"{Guid.NewGuid()}{extension}";
        var relativePath = Path.Combine(UploadsRoot, safeFileName);

        await using var fileStream = File.Create(relativePath);
        await content.CopyToAsync(fileStream, cancellationToken);

        return relativePath.Replace('\\', '/');
    }

    public Task<(Stream Content, string ContentType)?> GetAsync(string relativePath, CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(relativePath);
        var uploadsFullPath = Path.GetFullPath(UploadsRoot);

        // Path traversal guard: the resolved path must stay inside the uploads root.
        if (!fullPath.StartsWith(uploadsFullPath, StringComparison.OrdinalIgnoreCase) || !File.Exists(fullPath))
        {
            return Task.FromResult<(Stream, string)?>(null);
        }

        var contentType = Path.GetExtension(fullPath).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream",
        };

        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult<(Stream, string)?>((stream, contentType));
    }

    public void Delete(string relativePath)
    {
        var fullPath = Path.GetFullPath(relativePath);
        var uploadsFullPath = Path.GetFullPath(UploadsRoot);

        if (fullPath.StartsWith(uploadsFullPath, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }

    private static bool IsWebp(byte[] header) =>
        header[0] == 'R' && header[1] == 'I' && header[2] == 'F' && header[3] == 'F'
        && header[8] == 'W' && header[9] == 'E' && header[10] == 'B' && header[11] == 'P';
}
