using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using App.DAL.EF;
using App.Domain.Identity;
using App.DTO.v1_0;
using App.DTO.v1_0.Identity;
using Asp.Versioning;
using Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace RecipeApp.ApiControllers.Identity;

[ApiVersion("1.0")]
[Route("/api/v{version:apiVersion}/[controller]/[action]")]
[ApiController]
public class AccountController(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    ILogger<AccountController> logger,
    IConfiguration configuration,
    AppDbContext context
) : ControllerBase
{
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<RestApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<RestApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LoginResponse>> Register(
        [FromBody] RegisterRequest request,
        [FromQuery] int expiresInSeconds)
    {
        expiresInSeconds = GetValidExpiration(expiresInSeconds);

        // is user already registered
        AppUser? user = await userManager.FindByEmailAsync(request.Email);
        if (user != null)
        {
            logger.LogWarning("User with email {email} is already registered", request.Email);
            return BadRequest(
                new RestApiErrorResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Error = $"User with email {request.Email} is already registered"
                }
            );
        }

        user = await userManager.FindByNameAsync(request.Username);
        if (user != null)
        {
            logger.LogWarning("User with username {username} is already registered", request.Username);
            return BadRequest(
                new RestApiErrorResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Error = $"User with username {request.Username} is already registered"
                }
            );
        }

        // register user
        var refreshToken = new AppRefreshToken();
        user = new AppUser
        {
            Email = request.Email,
            UserName = request.Username,
            RefreshTokens = new List<AppRefreshToken> { refreshToken }
        };
        refreshToken.AppUser = user;

        IdentityResult result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(
                new RestApiErrorResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Error = result.Errors.First().Description
                }
            );
        }

        // get full user from system with fixed data (maybe there is something generated by identity that we might need
        user = await userManager.FindByEmailAsync(user.Email);
        if (user == null)
        {
            logger.LogError("User with email {email} is not found after registration", request.Email);
            return BadRequest(
                new RestApiErrorResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Error = $"User with email {request.Email} is not found after registration"
                }
            );
        }

        return Ok(new LoginResponse
        {
            JsonWebToken = await CreateJwt(user, expiresInSeconds),
            RefreshToken = refreshToken.RefreshToken,
            Username = user.UserName!,
            Email = user.Email!
        });
    }

    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<RestApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<RestApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest loginRequest,
        [FromQuery] int expiresInSeconds
    )
    {
        expiresInSeconds = GetValidExpiration(expiresInSeconds);

        AppUser? user;
        
        var emailValidator = new EmailAddressAttribute();
        var isEmail = emailValidator.IsValid(loginRequest.UsernameOrEmail);

        if (isEmail)
        {
            user = await userManager.FindByEmailAsync(loginRequest.UsernameOrEmail);
            if (user == null)
                logger.LogWarning("{User} with email {Email} not found", nameof(AppUser), loginRequest.UsernameOrEmail);
        }
        else
        {
            user = await userManager.FindByNameAsync(loginRequest.UsernameOrEmail);
            if (user == null)
                logger.LogWarning("{User} with username {Username} not found", nameof(AppUser), loginRequest.UsernameOrEmail);
        }

        if (user == null)
        {
            // TODO: random delay
            return BadRequest(
                new RestApiErrorResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Error = "Invalid login attempt"
                });
        }

        SignInResult result =
            await signInManager.CheckPasswordSignInAsync(user, loginRequest.Password, lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            // TODO: random delay
            logger.LogWarning("Failed login attempt for {Username}", user.UserName);
            return BadRequest("Invalid login attempt");
        }

        var deletedRows = await context.RefreshTokens
            .Where(t => t.AppUserId == user.Id && t.ExpirationDateTime < DateTime.UtcNow)
            .ExecuteDeleteAsync();
        logger.LogInformation("Deleted {DeletedRows} expired refresh tokens", deletedRows);

        var refreshToken = new AppRefreshToken
        {
            AppUserId = user.Id
        };

        context.RefreshTokens.Add(refreshToken);
        await context.SaveChangesAsync();

        return Ok(new LoginResponse
        {
            JsonWebToken = await CreateJwt(user, expiresInSeconds),
            RefreshToken = refreshToken.RefreshToken,
            Username = user.UserName!,
            Email = user.Email!
        });
    }

    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<RestApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<RestApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LoginResponse>> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        [FromQuery] int expiresInSeconds
    )
    {
        if (request.JsonWebToken == null)
        {
            return BadRequest(
                new RestApiErrorResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Error = "No token"
                }
            );
        }
        
        // extract jwt object
        JwtSecurityToken securityToken = new JwtSecurityTokenHandler().ReadJwtToken(request.JsonWebToken);
        expiresInSeconds = GetValidExpiration(expiresInSeconds);

        // validate jwt, ignore expiration date
        if (!IdentityHelpers.IsJwtValid(
                request.JsonWebToken,
                configuration.GetValue<string>("JWT:key")!,
                configuration.GetValue<string>("JWT:issuer")!,
                configuration.GetValue<string>("JWT:audience")!
            ))
        {
            return BadRequest("Invalid token");
        }

        var userEmail = securityToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;
        if (userEmail == null)
        {
            return BadRequest(
                new RestApiErrorResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Error = "No email claim"
                }
            );
        }

        AppUser? user = await userManager.FindByEmailAsync(userEmail);
        if (user == null)
        {
            return NotFound($"User with email {userEmail} not found");
        }

        // load and compare refresh tokens
        await context.Entry(user)
            .Collection(u => u.RefreshTokens!)
            .Query()
            .Where(t =>
                (t.RefreshToken == request.RefreshToken && t.ExpirationDateTime > DateTime.UtcNow) ||
                (t.PreviousRefreshToken == request.RefreshToken &&
                 t.PreviousExpirationDateTime > DateTime.UtcNow)
            ).ToListAsync();

        if (user.RefreshTokens == null || user.RefreshTokens.Count == 0)
        {
            return NotFound(
                new RestApiErrorResponse
                {
                    Status = HttpStatusCode.NotFound,
                    Error = $"RefreshTokens collection is {(user.RefreshTokens == null ? "null" : "empty")}"
                }
            );
        }

        if (user.RefreshTokens.Count != 1)
        {
            return BadRequest(
                new RestApiErrorResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Error = "More than one valid refresh token found"
                }
            );
        }

        // make new refresh token, keep old one still valid for some time
        AppRefreshToken userRefreshToken = user.RefreshTokens.First();
        if (userRefreshToken.RefreshToken == request.RefreshToken)
        {
            userRefreshToken.PreviousRefreshToken = userRefreshToken.RefreshToken;
            userRefreshToken.PreviousExpirationDateTime = DateTime.UtcNow.AddMinutes(1);

            userRefreshToken.RefreshToken = Guid.NewGuid().ToString();
            userRefreshToken.ExpirationDateTime = DateTime.UtcNow.AddDays(7);

            await context.SaveChangesAsync();
        }

        return Ok(new LoginResponse
        {
            JsonWebToken = await CreateJwt(user, expiresInSeconds),
            RefreshToken = userRefreshToken.RefreshToken
        });
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType<LogoutResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<RestApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<RestApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LogoutResponse>> Logout([FromBody] LogoutRequest request)
    {
        // delete the refresh token - so user is kicked out after jwt expiration
        // We do not invalidate the jwt on serverside - that would require pipeline modification and checking against db on every request
        // so client can actually continue to use the jwt until it expires (keep the jwt expiration time short ~1 min)

        var userIdStr = userManager.GetUserId(User);
        if (userIdStr == null)
        {
            return BadRequest(
                new RestApiErrorResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Error = "Invalid refresh token"
                }
            );
        }

        if (!Guid.TryParse(userIdStr, out Guid userId))
        {
            return BadRequest(
                new RestApiErrorResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Error = "Invalid user id"
                }
            );
        }

        AppUser? user = await context.Users
            .Where(u => u.Id == userId)
            .SingleOrDefaultAsync();
        if (user == null)
        {
            return NotFound(
                new RestApiErrorResponse
                {
                    Status = HttpStatusCode.NotFound,
                    Error = "User not found"
                }
            );
        }

        await context.Entry(user)
            .Collection(u => u.RefreshTokens!)
            .Query()
            .Where(t =>
                t.RefreshToken == request.RefreshToken ||
                t.PreviousRefreshToken == request.RefreshToken
            )
            .ToListAsync();

        foreach (AppRefreshToken appRefreshToken in user.RefreshTokens!)
        {
            context.RefreshTokens.Remove(appRefreshToken);
        }

        var deleteCount = await context.SaveChangesAsync();

        return Ok(new LogoutResponse
        {
            DeletedTokens = deleteCount
        });
    }

    private async Task<string> CreateJwt(AppUser user, int expiresInSeconds)
    {
        ClaimsPrincipal claimsPrincipal = await signInManager.CreateUserPrincipalAsync(user);
        var token = IdentityHelpers.GenerateJwt(
            claimsPrincipal.Claims,
            configuration.GetValue<string>("JWT:key")!,
            configuration.GetValue<string>("JWT:issuer")!,
            configuration.GetValue<string>("JWT:audience")!,
            expiresInSeconds
        );
        return token;
    }

    private int GetValidExpiration(int expiresInSeconds)
    {
        var maxSeconds = configuration.GetValue<int>("JWT:expiresInSeconds");
        if (expiresInSeconds <= 0 || expiresInSeconds > maxSeconds) expiresInSeconds = maxSeconds;
        return expiresInSeconds;
    }
}