using eAutoskola.Application.Users;
using eAutoskola.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eAutoskola.API.Controllers;

[Route("api/users")]
[Authorize(Roles = Role.Admin)]
public class UserController : BaseCRUDController<UserDto, UserSearchObject, UserInsertRequest, UserUpdateRequest>
{
    private readonly UserService _userService;

    public UserController(UserService userService) : base(userService)
    {
        _userService = userService;
    }

    [HttpPost("{id:int}/activate")]
    public async Task<IActionResult> Activate(int id, CancellationToken cancellationToken)
    {
        await _userService.SetActiveAsync(id, true, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        await _userService.SetActiveAsync(id, false, cancellationToken);
        return NoContent();
    }
}
