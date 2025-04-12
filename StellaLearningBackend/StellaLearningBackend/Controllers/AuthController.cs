using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using StellaLearningBackend.Models;
using StellaLearning.Dtos; // Your Models namespace (for ApplicationUser)

namespace StellaLearningBackend.Controllers;

[Route("api/[controller]")] // Sets the base route to /api/auth
[ApiController]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager; // Optional: Only needed if using cookie-based sign-in methods directly, but useful for CheckPasswordSignInAsync
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger; // Good practice to add logging

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _logger = logger;
    }

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
            _logger.LogWarning("Registration failed: Email {Email} already exists.", registerDto.Email);
            // Return a generic error to avoid revealing if an email is registered
            return BadRequest(new { Message = "Registration failed: Email {Email} already exists."});
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
            _logger.LogError("User creation failed for {Email}. Errors: {Errors}",
                registerDto.Email,
                string.Join(", ", result.Errors.Select(e => e.Description)));

            // Add specific errors to ModelState for detailed feedback (optional, consider security implications)
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            // Return a generic error or the ModelState containing specific errors
            return BadRequest(new { Message = "User creation failed.", Errors = result.Errors.Select(e => e.Description) });
        }

        _logger.LogInformation("User {Email} created successfully.", registerDto.Email);

        // Optional: Assign a default role here if needed using _userManager.AddToRoleAsync(newUser, "User");

        // Return 201 Created status without revealing sensitive info
        // Could return the created user ID or email if desired, but often not necessary here.
        return StatusCode(StatusCodes.Status201Created, new { Message = "User registered successfully." });
    }


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
        var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: false); // Set lockoutOnFailure to true if desired

        if (result.IsNotAllowed) // Check specifically for confirmation/lockout issues
        {
            _logger.LogWarning("Login failed: Account for {Email} is not allowed (e.g., needs confirmation).", loginDto.Email);
            return Unauthorized(new
            {
                Message = "Account not confirmed or locked out."
            });
        }

        if (!result.Succeeded)
        {
            _logger.LogWarning("Login failed: Invalid password for user {Email}.", loginDto.Email);
            // Check for specific results like IsLockedOut, RequiresTwoFactor if needed
            return Unauthorized(new { Message = "Invalid password." }); // Generic message
        }

        _logger.LogInformation("User {Email} logged in successfully.", loginDto.Email);

        // Generate JWT Token
        var (Token, Expiration) = GenerateJwtToken(user);

        return Ok(new AuthResponseDto
        {
            UserId = user.Id,
            Email = user.Email!, // User email should not be null if found
            Token = Token,
            TokenExpiration = Expiration
            // Map other properties if needed:
            // FullName = user.FullName
        });
    }


    // --- Helper Method for JWT Generation ---
    private (string Token, DateTime Expiration) GenerateJwtToken(ApplicationUser user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        // Claims identify the user and token properties
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id), // Subject (usually user ID)
            new(JwtRegisteredClaimNames.Email, user.Email!),
            // Jti is a unique identifier for the token, useful for revocation if needed
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            // Add custom claims if necessary (e.g., roles)
            // var userRoles = await _userManager.GetRolesAsync(user);
            // foreach (var role in userRoles) { claims.Add(new Claim(ClaimTypes.Role, role)); }
        };

        // Get expiration time from config or set a default (e.g., 1 hour)
        var expirationMinutes = _configuration.GetValue<int>("Jwt:ExpirationMinutes", 60);
        var expiration = DateTime.UtcNow.AddMinutes(expirationMinutes);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiration,
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return (tokenString, expiration);
    }
}