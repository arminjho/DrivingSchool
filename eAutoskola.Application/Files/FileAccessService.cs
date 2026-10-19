using eAutoskola.Domain.Exceptions;
using eAutoskola.Domain.Resources;
using eAutoskola.Domain.Security;
using eAutoskola.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Application.Files;

/// <summary>
/// Decides who may download a stored image (upute.md §5: file download needs authorization AND
/// an ownership check). The path must belong to a record the caller is allowed to see:
///  - Admin: any file;
///  - a profile photo: its owner; instructors' photos are shown to every signed-in user (booking
///    and lesson screens); a student's photo is also visible to an instructor who teaches them;
///  - vehicle photos and test-question images are shared training content for every signed-in user;
///  - a file no record points to is not served to anyone but an Admin.
/// </summary>
public class FileAccessService
{
    private readonly EAutoskolaDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public FileAccessService(EAutoskolaDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task EnsureCanReadAsync(string path, CancellationToken cancellationToken)
    {
        if (_currentUser.IsInRole(Role.Admin))
        {
            return;
        }

        var storedPath = path.Replace('\\', '/');
        var callerId = _currentUser.UserId;

        var photoOwner = await _context.Users.AsNoTracking()
            .Where(u => u.ProfileImagePath == storedPath)
            .Select(u => new { u.Id, u.Role })
            .FirstOrDefaultAsync(cancellationToken);

        if (photoOwner is not null)
        {
            if (photoOwner.Id == callerId || photoOwner.Role == Role.Instructor)
            {
                return;
            }

            var instructorTeachesOwner = _currentUser.IsInRole(Role.Instructor)
                && await _context.Lessons.AnyAsync(l => l.InstructorId == callerId && l.StudentId == photoOwner.Id, cancellationToken);
            if (instructorTeachesOwner)
            {
                return;
            }

            throw new ForbiddenException(Messages.FileAccessDenied);
        }

        var isSharedContent = await _context.Vehicles.AnyAsync(v => v.ImagePath == storedPath, cancellationToken)
            || await _context.Questions.AnyAsync(q => q.ImagePath == storedPath, cancellationToken);
        if (!isSharedContent)
        {
            throw new ForbiddenException(Messages.FileAccessDenied);
        }
    }
}
