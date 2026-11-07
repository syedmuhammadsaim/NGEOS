using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NGEOS.Data;
using NGEOS.Helpers;
using NGEOS.Models.ApiDtos;
using NGEOS.Services;

namespace NGEOS.Controllers.Api
{
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IJwtTokenService _jwtService;
        private readonly ILogger<AuthApiController> _logger;

        public AuthApiController(ApplicationDbContext context, IJwtTokenService jwtService, ILogger<AuthApiController> logger)
        {
            _context = context;
            _jwtService = jwtService;
            _logger = logger;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new LoginResponseDto { Success = false, Message = "Email and Password are required." });
            }

            var normalizedEmail = request.Email.Trim().ToLower();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (user == null || !PasswordHelper.VerifyPassword(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Failed API login attempt for {Email}", request.Email);
                return Unauthorized(new LoginResponseDto { Success = false, Message = "Invalid email or password." });
            }

            if (!user.IsActive)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new LoginResponseDto { Success = false, Message = "This account has been deactivated." });
            }

            // Ensure role is strictly Admin or User
            var role = user.Role == "Admin" ? "Admin" : "User";
            var token = _jwtService.GenerateToken(user);

            _logger.LogInformation("API user logged in: {Email} with role {Role}", user.Email, role);

            return Ok(new LoginResponseDto
            {
                Success = true,
                Message = $"Welcome back, {user.FullName}!",
                Token = token,
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = role,
                ProfileImageUrl = user.ProfileImageUrl,
                Phone = user.Phone,
                Address = user.Address,
                Profession = user.Profession,
                Organization = user.Organization
            });
        }

        [HttpGet("me")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<ActionResult<LoginResponseDto>> GetCurrentUser()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new LoginResponseDto { Success = false, Message = "Invalid user token." });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsActive)
            {
                return Unauthorized(new LoginResponseDto { Success = false, Message = "User not found or inactive." });
            }

            return Ok(new LoginResponseDto
            {
                Success = true,
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role == "Admin" ? "Admin" : "User",
                ProfileImageUrl = user.ProfileImageUrl,
                Phone = user.Phone,
                Address = user.Address,
                Profession = user.Profession,
                Organization = user.Organization
            });
        }
    }
}
