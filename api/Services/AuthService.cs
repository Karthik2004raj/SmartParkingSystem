using Microsoft.Extensions.Logging; // Added for ILogger
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SmartParking.API.Models;

namespace SmartParking.API.Services
{
    /// <summary>
    /// Defines authentication-related operations: JWT generation, password verification, hashing.
    /// </summary>
    public interface IAuthService
    {
        string GenerateJwtToken(User user);
        bool VerifyPassword(string password, string passwordHash);
        string HashPassword(string password);
    }

    /// <summary>
    /// Implementation of IAuthService using JWT and BCrypt.
    /// Handles secure password hashing and token generation.
    /// </summary>
    public class AuthService : IAuthService
    {
        // Configuration for JWT settings (key, issuer, audience)
        private readonly IConfiguration _configuration;

        // Logger for capturing runtime information and errors
        private readonly ILogger<AuthService> _logger;

        /// <summary>
        /// Constructor: Injects dependencies via Dependency Injection (DI).
        /// - IConfiguration: Provides access to appsettings.json (JWT secret, issuer, audience).
        /// - ILogger<AuthService>: Enables structured logging for this service.
        /// DI allows loose coupling and makes the service testable by injecting mock configuration.
        /// </summary>
        /// <param name="configuration">Configuration containing JWT settings.</param>
        /// <param name="logger">Logger for this service.</param>
        public AuthService(IConfiguration configuration, ILogger<AuthService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Generates a JWT token for a given user, containing claims (UserId, Username, Role).
        /// The token expires after 8 hours and is signed with a symmetric key from configuration.
        /// </summary>
        /// <param name="user">User object with Id, Username, Role.</param>
        /// <returns>JWT token as a string.</returns>
        public string GenerateJwtToken(User user)
        {
            try
            {
                _logger.LogInformation("Generating JWT token for user: {Username}", user.Username);

                var tokenHandler = new JwtSecurityTokenHandler();

                // Retrieve the secret key from configuration; fallback to a default for development
                var jwtKey = _configuration["Jwt:Key"];
                if (string.IsNullOrEmpty(jwtKey))
                {
                    _logger.LogWarning("JWT key not found in configuration. Using fallback default key.");
                    jwtKey = "YourSuperSecretKeyForJWTTokenGeneration2024!@#$%SuperSecretKey";
                }
                var key = Encoding.ASCII.GetBytes(jwtKey);

                // Build the token descriptor with claims and expiration
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.Role, user.Role)
                    }),
                    Expires = DateTime.UtcNow.AddHours(8),
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256Signature),
                    Issuer = _configuration["Jwt:Issuer"],
                    Audience = _configuration["Jwt:Audience"]
                };

                // Create and return the token
                var token = tokenHandler.CreateToken(tokenDescriptor);
                return tokenHandler.WriteToken(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate JWT token for user: {Username}", user?.Username);
                // Rethrow the exception so the calling controller can handle it gracefully.
                // Alternatively, we could return null, but rethrowing allows a unified error response.
                throw new Exception("An error occurred while generating the authentication token.", ex);
            }
        }

        /// <summary>
        /// Verifies a plain password against a BCrypt hash.
        /// </summary>
        /// <param name="password">Plain password to verify.</param>
        /// <param name="passwordHash">Stored BCrypt hash.</param>
        /// <returns>True if password matches the hash; otherwise false.</returns>
        public bool VerifyPassword(string password, string passwordHash)
        {
            try
            {
                // BCrypt.Verify returns true if the password matches the hash.
                // This is a synchronous operation, no try-catch needed but we add for safety.
                return BCrypt.Net.BCrypt.Verify(password, passwordHash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password verification.");
                // Return false on any error to prevent authentication bypass.
                return false;
            }
        }

        /// <summary>
        /// Hashes a plain password using BCrypt with a salt (automatically generated).
        /// </summary>
        /// <param name="password">Plain password to hash.</param>
        /// <returns>BCrypt hash string.</returns>
        public string HashPassword(string password)
        {
            try
            {
                _logger.LogInformation("Hashing password (length: {Length}).", password?.Length ?? 0);
                return BCrypt.Net.BCrypt.HashPassword(password);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hashing password.");
                // Rethrow to indicate failure; caller should handle.
                throw new Exception("An error occurred while hashing the password.", ex);
            }
        }
    }
}