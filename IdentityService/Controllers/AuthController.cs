using IdentityService.Models.DTOs;
using IdentityService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


namespace IdentityService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // POST api/auth/register
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var result = await _authService.RegisterAsync(request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST api/auth/login
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                var result = await _authService.LoginAsync(request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET api/auth/user/{id}
        [HttpGet("user/{id:guid}")]
        public async Task<ActionResult<UserResponse>> GetUserById(Guid id)
        {
            try
            {
                var user = await _authService.GetUserByIdAsync(id);
                if (user == null) return NotFound();
                return Ok(user);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET api/auth/user/all
        [HttpGet("user/all")]
        public async Task<ActionResult<IReadOnlyList<UserResponse>>> GetUsers()
        {
            try
            {
                var users = await _authService.GetAllUsersAsync();
                if (users == null) return NotFound();
                return Ok(users);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET api/auth/user/telegram/{telegramId}
        [HttpGet("user/telegram/{telegramId:long}")]

        public async Task<ActionResult<UserResponse>> GetUserByTelegramId(long telegramId)
        {
            try
            {
                var user = await _authService.GetUserByTelegramIdAsync(telegramId);
                if (user == null) return NotFound();
                return Ok(user);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT api/auth/user/update_profile
        [HttpPut("user/update_profile")]
        [Authorize]
        public async Task<ActionResult<UserResponse>> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            try
            {
                // Получаем ID из JWT токена
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
                    return Unauthorized();

                var user = await _authService.UpdateProfileAsync(userId, request);
                return Ok(user);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT api/auth/user/change_role
        [HttpPut("user/change_role")]
        public async Task<ActionResult<UserResponse>> ChangeRole(ChangeRoleRequest request)
        {
            try
            {
                var result = await _authService.ChangeRoleAsync(request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}