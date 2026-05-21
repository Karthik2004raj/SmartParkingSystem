using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartParking.API.Data;
using SmartParking.API.DTOs;
using SmartParking.API.Services;

namespace SmartParking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuthService _authService;
        
        public AuthController(ApplicationDbContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }
        
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
                
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == loginDto.Username && u.IsActive);
                
            if (user == null || !_authService.VerifyPassword(loginDto.Password, user.PasswordHash))
                return Unauthorized(new { message = "Invalid username or password" });
                
            var token = _authService.GenerateJwtToken(user);
            
            return Ok(new LoginResponseDto
            {
                Token = token,
                Username = user.Username,
                Role = user.Role,
                UserId = user.UserId
            });
        }
    }
}
