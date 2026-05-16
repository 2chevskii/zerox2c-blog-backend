using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZeroX2C.Blog.API.Modules.Users.Auth;
using ZeroX2C.Blog.API.Modules.Users.Contracts.Auth;

namespace ZeroX2C.Blog.API.Modules.Users.Controllers;

[ApiController, Route("api/auth")]
public sealed class AuthController(
    IAuthService authService,
    IAuthenticationContext authenticationContext
) : ControllerBase
{
    [HttpGet("steam")]
    [AllowAnonymous]
    public IActionResult LoginWithSteam()
    {
        var callbackUrl = GetSteamCallbackUrl();
        var realm = $"{Request.Scheme}://{Request.Host}";

        return Redirect(authService.CreateSteamAuthenticationUrl(callbackUrl, realm));
    }

    [HttpGet("steam/callback")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> SteamCallback(CancellationToken cancellationToken)
    {
        var callback = new SteamOpenIdCallback(
            Request.Query.ToDictionary(
                parameter => parameter.Key,
                parameter => parameter.Value.ToString()
            )
        );

        return ToAuthActionResult(
            await authService.LoginWithSteamAsync(
                callback,
                GetSteamCallbackUrl(),
                cancellationToken
            )
        );
    }

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
        if (authenticationContext.MaybeUserId is not { } userId)
        {
            return Unauthorized();
        }

        var result = await authService.GetCurrentUserAsync(userId, cancellationToken);
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
            AuthOperationStatus.ExternalLoginFailed => Unauthorized(),
            AuthOperationStatus.UserBlocked => Forbid(),
            _ => Problem(),
        };

    private string GetSteamCallbackUrl() =>
        Url.Action(
            nameof(SteamCallback),
            "Auth",
            values: null,
            protocol: Request.Scheme,
            host: Request.Host.ToString()
        )
        ?? throw new InvalidOperationException("Failed to build Steam callback URL.");
}
