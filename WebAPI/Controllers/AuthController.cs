using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AyanamisTower.WebAPI.Data;
using AyanamisTower.WebAPI.Dtos;
using AyanamisTower.WebAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Your Models namespace (for ApplicationUser)
using Microsoft.IdentityModel.Tokens;

namespace AyanamisTower.WebAPI.Controllers;

/// <summary>
/// Handles user authentication operations like registration, login, and token refresh.
/// </summary>
[Route("api/[controller]")] // Sets the base route to /api/auth
[ApiController]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager; // Optional: Only needed if using cookie-based sign-in methods directly, but useful for CheckPasswordSignInAsync
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger; // Good practice to add logging
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthController"/> class.
    /// </summary>
    /// <param name="userManager">The ASP.NET Core Identity user manager.</param>
    /// <param name="signInManager">The ASP.NET Core Identity sign-in manager.</param>
    /// <param name="configuration">The application configuration properties.</param>
    /// <param name="logger">The logger for this controller.</param>
    /// <param name="context">The database context.</param>
    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration,
        ILogger<AuthController> logger,
        ApplicationDbContext context
    )
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// Registers a new user in the system.
    /// </summary>
    /// <param name="registerDto">The user registration details.</param>
    /// <returns>An IActionResult indicating the result of the registration attempt.</returns>
    // POST: api/auth/register
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation("Registration attempt for {Email}", registerDto.Email);

        var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
        if (existingUser != null)
        {
            _logger.LogWarning(
                "Registration failed: Email {Email} already exists.",
                registerDto.Email
            );
            // Return a generic error to avoid revealing if an email is registered
            return BadRequest(
                new
                {
                    Message = $"Registration failed: Email {registerDto.Email} already exists.",
                    Errors = Array.Empty<List<string>>(),
                }
            );
        }

        var newUser = new ApplicationUser
        {
            Email = registerDto.Email,
            UserName = registerDto.Email,
            // Typically use email as username for simplicity
            // Set other properties from DTO if needed:
            // FullName = registerDto.FullName
        };

        var result = await _userManager.CreateAsync(newUser, registerDto.Password);

        if (!result.Succeeded)
        {
            _logger.LogError(
                "User creation failed for {Email}. Errors: {Errors}",
                registerDto.Email,
                string.Join(", ", result.Errors.Select(e => e.Description))
            );

            // Add specific errors to ModelState for detailed feedback (optional, consider security implications)
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            // Return a generic error or the ModelState containing specific errors
            return BadRequest(
                new
                {
                    Message = "User creation failed.",
                    Errors = result.Errors.Select(e => e.Description),
                }
            );
        }

        _logger.LogInformation("User {RegisterDto.Email} created successfully.", registerDto.Email);

        // Optional: Assign a default role here if needed using _userManager.AddToRoleAsync(newUser, "User");

        // Return 201 Created status without revealing sensitive info
        // Could return the created user ID or email if desired, but often not necessary here.
        return StatusCode(
            StatusCodes.Status201Created,
            new { Message = "User registered successfully." }
        );
    }

    /// <summary>
    /// Authenticates a user and returns JWT and refresh tokens upon successful login.
    /// </summary>
    /// <param name="loginDto">The user login credentials (email and password).</param>
    /// <returns>An IActionResult containing the authentication tokens or an error response.</returns>
    // POST: api/auth/login
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation("Login attempt for {Email}", loginDto.Email);

        var user = await _userManager.FindByEmailAsync(loginDto.Email);
        if (user == null)
        {
            _logger.LogWarning("Login failed: User {Email} not found.", loginDto.Email);
            return Unauthorized(new { Message = "Email not found" }); // Generic message
        }

        // Use CheckPasswordSignInAsync for password verification without setting auth cookies
        var result = await _signInManager.CheckPasswordSignInAsync(
            user,
            loginDto.Password,
            lockoutOnFailure: false
        ); // Set lockoutOnFailure to true if desired

        if (result.IsNotAllowed) // Check specifically for confirmation/lockout issues
        {
            _logger.LogWarning(
                "Login failed: Account for {Email} is not allowed (e.g., needs confirmation).",
                loginDto.Email
            );
            return Unauthorized(new { Message = "Account not confirmed or locked out." });
        }

        if (!result.Succeeded)
        {
            _logger.LogWarning("Login failed: Invalid password for user {Email}.", loginDto.Email);
            // Check for specific results like IsLockedOut, RequiresTwoFactor if needed
            return Unauthorized(new { Message = "Invalid password." }); // Generic message
        }

        _logger.LogInformation("User {Email} logged in successfully.", loginDto.Email);

        var refreshToken = GenerateRefreshToken();
        var refreshTokenExpirationDays = _configuration.GetValue(
            "Jwt:RefreshTokenExpirationDays",
            30
        ); // Default 30 days
        var refreshTokenEntity = new UserRefreshToken
        {
            UserId = user.Id,
            Token = refreshToken, // Store the actual token string
            ExpiresUtc = DateTime.UtcNow.AddDays(refreshTokenExpirationDays),
            // CreatedUtc is set by default
        };

        try
        {
            // Optional: Invalidate previous refresh tokens for this user if you only want one active session
            // var existingTokens = await _context.UserRefreshTokens
            //    .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
            //    .ToListAsync();
            // foreach (var token in existingTokens) { token.IsRevoked = true; }

            _context.UserRefreshTokens.Add(refreshTokenEntity);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Stored new refresh token for user {UserId}", user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store refresh token for user {UserId}", user.Id);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { Message = "An error occurred while processing your login." }
            );
        }

        // Generate JWT Token
        var (Token, Expiration) = GenerateJwtToken(user);

        return Ok(
            new AuthResponseDto
            {
                UserId = user.Id,
                Email = user.Email!,
                Token = Token,
                TokenExpiration = Expiration,
                RefreshToken = refreshToken,
                // Map other properties if needed:
                // FullName = user.FullName
            }
        );
    }

    /// <summary>
    /// Refreshes the authentication tokens using a valid refresh token.
    /// </summary>
    /// <param name="refreshRequestDto">The request containing the refresh token.</param>
    /// <returns>An IActionResult containing the new authentication tokens or an error response.</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto refreshRequestDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation("Token refresh attempt.");

        // --- Database Interaction (Validate Refresh Token) ---
        UserRefreshToken? storedToken;
        try
        {
            // Find the token in the database, include the User navigation property
            storedToken = await _context
                .UserRefreshTokens.Include(rt => rt.User) // Important: Load the related user data
                .FirstOrDefaultAsync(rt => rt.Token == refreshRequestDto.RefreshToken);

            if (storedToken == null)
            {
                _logger.LogWarning("Refresh token not found.");
                return Unauthorized(new { Message = "Invalid refresh token." });
            }

            if (storedToken.IsRevoked)
            {
                _logger.LogWarning(
                    "Refresh token is revoked. UserID: {UserId}",
                    storedToken.UserId
                );
                // Security consideration: If a revoked token is used, maybe revoke all tokens for that user?
                return Unauthorized(new { Message = "Refresh token has been revoked." });
            }

            if (storedToken.ExpiresUtc < DateTime.UtcNow)
            {
                _logger.LogWarning("Refresh token expired. UserID: {UserId}", storedToken.UserId);
                // Clean up expired tokens periodically with a background job
                return Unauthorized(new { Message = "Refresh token has expired." });
            }

            if (storedToken.User == null)
            {
                _logger.LogError(
                    "User associated with refresh token not found. UserID: {UserId}",
                    storedToken.UserId
                );
                // This shouldn't happen with proper DB constraints but handle defensively
                storedToken.IsRevoked = true; // Revoke the problematic token
                await _context.SaveChangesAsync();
                return Unauthorized(new { Message = "User associated with token not found." });
            }

            // Mark the used token as revoked (implementing rotation)
            storedToken.IsRevoked = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred during refresh token validation for token {Token}",
                refreshRequestDto.RefreshToken
            );
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { Message = "An error occurred while processing your request." }
            );
        }
        // --- End Database Interaction ---


        var user = storedToken.User; // Get the user from the validated token

        // --- Generate NEW Access Token ---
        var (newAccessToken, newAccessTokenExpiration) = GenerateJwtToken(user);

        // --- Generate NEW Refresh Token (Rotation) ---
        var newRefreshToken = GenerateRefreshToken();
        var refreshTokenExpirationDays = _configuration.GetValue(
            "Jwt:RefreshTokenExpirationDays",
            30
        );
        var newRefreshTokenEntity = new UserRefreshToken
        {
            UserId = user.Id,
            Token = newRefreshToken,
            ExpiresUtc = DateTime.UtcNow.AddDays(refreshTokenExpirationDays),
        };

        // --- Database Interaction (Store New Refresh Token, Save Revocation) ---
        try
        {
            _context.UserRefreshTokens.Add(newRefreshTokenEntity);
            await _context.SaveChangesAsync(); // Saves both the new token and the revocation of the old one
            _logger.LogInformation("Refreshed tokens successfully for user {UserId}", user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to store new refresh token after refresh for user {UserId}",
                user.Id
            );
            // Don't return the new access token if we couldn't store the new refresh token state
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { Message = "An error occurred while finalizing token refresh." }
            );
        }
        // --- End Database Interaction ---

        // --- Return NEW tokens ---
        return Ok(
            new AuthResponseDto
            {
                UserId = user.Id,
                Email = user.Email!,
                Token = newAccessToken,
                TokenExpiration = newAccessTokenExpiration,
                RefreshToken =
                    newRefreshToken // Return the NEW refresh token
                ,
            }
        );
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[64]; // Increase size for more entropy (e.g., 64 bytes)
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
            // Return a URL-safe base64 string
            return Convert.ToBase64String(randomNumber).Replace("+", "-").Replace("/", "_");
        }
    }

    private (string Token, DateTime Expiration) GenerateJwtToken(ApplicationUser user)
    {
        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!)
        );
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        // Claims identify the user and token properties
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id), // Subject (usually user ID)
            new(JwtRegisteredClaimNames.Email, user.Email!),
            // Jti is a unique identifier for the token, useful for revocation if needed
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            // Add custom claims if necessary (e.g., roles)
            // var userRoles = await _userManager.GetRolesAsync(user);
            // foreach (var role in userRoles) { claims.Add(new Claim(ClaimTypes.Role, role)); }
        };

        // Get expiration time from config or set a default (e.g., 1 hour)
        var expirationMinutes = _configuration.GetValue("Jwt:ExpirationMinutes", 60);
        var expiration = DateTime.UtcNow.AddMinutes(expirationMinutes);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiration,
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            SigningCredentials = credentials,
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return (tokenString, expiration);
    }
}
