using eAutoskola.Application.Files;
using eAutoskola.Domain.Files;
using eAutoskola.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eAutoskola.API.Controllers;

/// <summary>Serves and accepts images saved via IFileStorageService (profile pictures,
/// vehicle and question images). [Authorize] rather than static-file middleware, so a stored path
/// is only reachable by a signed-in client, and FileAccessService additionally checks that the
/// caller may see that particular file (upute.md §5: authorization + ownership on download).
/// Path-traversal is rejected inside the storage service itself.</summary>
[ApiController]
[Route("api/files")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly IFileStorageService _storage;
    private readonly FileAccessService _access;

    public FilesController(IFileStorageService storage, FileAccessService access)
    {
        _storage = storage;
        _access = access;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string path, CancellationToken cancellationToken)
    {
        await _access.EnsureCanReadAsync(path, cancellationToken);
        var file = await _storage.GetAsync(path, cancellationToken);
        return file is null ? NotFound() : File(file.Value.Content, file.Value.ContentType);
    }

    /// <summary>Generic image upload for entities with no self-service upload route of their
    /// own (e.g. a Question's "Slika znaka"), used from the admin screens that edit them.
    /// Admin-only - a Student/Instructor uploads their own profile picture via
    /// POST /api/me/profile-image instead, which is scoped to the caller's own record.</summary>
    [HttpPost("upload")]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult<UploadedFileDto>> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var path = await _storage.SaveImageAsync(stream, file.FileName, file.ContentType, cancellationToken);
        return Ok(new UploadedFileDto(path));
    }
}

public record UploadedFileDto(string Path);
