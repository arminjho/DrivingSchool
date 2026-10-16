namespace eAutoskola.Domain.Files;

public interface IFileStorageService
{
    /// <summary>Validates MIME type + magic bytes + size limit, generates a safe unique file
    /// name, and saves the file. Returns the relative path to store on the entity. Throws
    /// ValidationException (Bosnian message) if the file fails validation.</summary>
    Task<string> SaveImageAsync(Stream content, string originalFileName, string contentType, CancellationToken cancellationToken);

    /// <summary>Authorization/ownership is checked by the caller before this is invoked - this
    /// just resolves a stored relative path to bytes for download.</summary>
    Task<(Stream Content, string ContentType)?> GetAsync(string relativePath, CancellationToken cancellationToken);

    void Delete(string relativePath);
}
