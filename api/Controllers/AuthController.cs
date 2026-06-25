using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // Added for ILogger
using SmartParking.API.Data;
using SmartParking.API.DTOs;
using SmartParking.API.Services;

namespace SmartParking.API.Controllers
{
    /// <summary>
    /// Handles authentication-related API requests (login, JWT generation).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        // Database context for accessing user data
        private readonly ApplicationDbContext _context;
        
        // Authentication service for password verification and JWT generation
        private readonly IAuthService _authService;
        
        // Logger for capturing runtime information and errors
        private readonly ILogger<AuthController> _logger;

        /// <summary>
        /// Constructor: Injects dependencies via Dependency Injection (DI).
        /// - ApplicationDbContext: Provides database access (DbContext).
        /// - IAuthService: Handles password hashing/verification and JWT creation.
        /// - ILogger<AuthController>: Enables structured logging for this controller.
        /// DI reduces coupling and makes the controller testable.
        /// </summary>
        /// <param name="context">Database context for user queries.</param>
        /// <param name="authService">Service for authentication logic.</param>
        /// <param name="logger">Logger for this controller.</param>
        public AuthController(
            ApplicationDbContext context,
            IAuthService authService,
            ILogger<AuthController> logger)
        {
            _context = context;
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Authenticates a user and returns a JWT token on success.
        /// </summary>
        /// <param name="loginDto">Login credentials (username and password).</param>
        /// <returns>JWT token with user info on success; error details on failure.</returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                // Log the start of the login attempt (without password)
                _logger.LogInformation("Login attempt for username: {Username}", loginDto.Username);

                // Validate the incoming model (e.g., required fields, format)
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid login model for username: {Username}", loginDto.Username);
                    return BadRequest(ModelState);
                }

                // Query the database for an active user with the given username
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == loginDto.Username && u.IsActive);

                // If user not found or password verification fails, return 401 Unauthorized
                if (user == null || !_authService.VerifyPassword(loginDto.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Invalid credentials for username: {Username}", loginDto.Username);
                    return Unauthorized(new { message = "Invalid username or password" });
                }

                // Generate a JWT token for the authenticated user
                var token = _authService.GenerateJwtToken(user);

                // Prepare the success response with token and user details
                var response = new LoginResponseDto
                {
                    Token = token,
                    Username = user.Username,
                    Role = user.Role,
                    UserId = user.UserId
                };

                _logger.LogInformation("Login successful for username: {Username}", loginDto.Username);
                return Ok(response);
            }
            catch (Exception ex)
            {
                // Catch any unexpected exceptions (database errors, service failures, etc.)
                _logger.LogError(ex, "Login failed for username: {Username}", loginDto.Username);
                // Return a generic 500 error without exposing internal details
                return StatusCode(500, new { message = "An internal error occurred during login. Please try again later." });
            }
        }
    }
}