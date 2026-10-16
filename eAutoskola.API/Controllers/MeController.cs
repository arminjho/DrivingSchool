using eAutoskola.Application.Me;
using eAutoskola.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eAutoskola.API.Controllers;

[ApiController]
[Route("api/me")]
[Authorize]
public class MeController : ControllerBase
{
    private readonly MeService _meService;

    public MeController(MeService meService)
    {
        _meService = meService;
    }

    [HttpGet]
    public async Task<ActionResult<UserDto>> Get(CancellationToken cancellationToken)
        => Ok(await _meService.GetMeAsync(cancellationToken));

    [HttpPut]
    public async Task<ActionResult<UserDto>> Update(MeUpdateRequest request, CancellationToken cancellationToken)
        => Ok(await _meService.UpdateMeAsync(request, cancellationToken));

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await _meService.ChangePasswordAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpPost("profile-image")]
    public async Task<ActionResult<UserDto>> UploadProfileImage(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var result = await _meService.UploadProfileImageAsync(stream, file.FileName, file.ContentType, cancellationToken);
        return Ok(result);
    }
}
