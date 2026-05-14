using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZeroX2C.Blog.API.Modules.Users.Auth.Contracts;

namespace ZeroX2C.Blog.API.Modules.Users.Auth;

[ApiController, Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(
        RegisterUserRequest request,
        CancellationToken cancellationToken
    ) =>
        ToAuthActionResult(
            await authService.RegisterAsync(request, cancellationToken)
        );

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken
    ) =>
        ToAuthActionResult(
            await authService.LoginAsync(request, cancellationToken)
        );

    [HttpGet("/api/me")]
    [Authorize]
    public async Task<ActionResult<CurrentUserResponse>> Me(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            return Unauthorized();
        }

        var result = await authService.GetCurrentUserAsync(parsedUserId, cancellationToken);
        return result.Status switch
        {
            AuthOperationStatus.Success => result.Value!,
            AuthOperationStatus.UserNotFound => Unauthorized(),
            _ => Problem(),
        };
    }

    private ActionResult<AuthResponse> ToAuthActionResult(
        AuthOperationResult<AuthResponse> result
    ) =>
        result.Status switch
        {
            AuthOperationStatus.Success => result.Value!,
            AuthOperationStatus.UsernameAlreadyTaken => ValidationProblem(
                "Username is already taken."
            ),
            AuthOperationStatus.EmailAlreadyTaken => ValidationProblem(
                "Email is already taken."
            ),
            AuthOperationStatus.InvalidCredentials => Unauthorized(),
            _ => Problem(),
        };
}
