using IdentityService.DTO;
using IdentityService.Models.DTOs;
using IdentityService.Services;
using IdentityService.Services.RabbitMQ;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Authentication;
using System.Security.Claims;


namespace IdentityService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IRabbitMQPublisher _rabbitMQPublisher;

        public AuthController(IAuthService authService, IRabbitMQPublisher rabbitMQPublisher)
        {
            _authService = authService;
            _rabbitMQPublisher = rabbitMQPublisher;
        }
        private async Task LogToServiceAsync(string level, string eventType, string message)
        {
            await _rabbitMQPublisher.PublishAsync("logging_service_queue", new LogEventDto(level, eventType, message));
        }

        // POST api/auth/register
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var result = await _authService.RegisterAsync(request);
                await LogToServiceAsync("Information", "registration", $"Successfull registration of user {request.Username}");
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                await LogToServiceAsync("Error", "already-registered", $"User {request.Username} already has a profile");
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST api/auth/login
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                await LogToServiceAsync("Information", "login", $"Login of user {request.TelegramId} is successfull");
                var result = await _authService.LoginAsync(request);
                return Ok(result);
            }
            catch (NullReferenceException ex)
            {
                await LogToServiceAsync("Error", "user-not-found", $"User {request.TelegramId} not found");
                return BadRequest(new { message = ex.Message });
            }
            catch (AuthenticationException ex)
            {
                await LogToServiceAsync("Error", "incorrect-password", $"Password of user {request.TelegramId} is incorrect");
                return BadRequest(new { message = ex.Message });
            }
            catch (AccessViolationException ex)
            {
                await LogToServiceAsync("Error", "profile-deactivated", $"User {request.TelegramId} profile is inactive");
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET api/auth/user/{id}
        [HttpGet("user/{id:guid}")]
        [Authorize]
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
        //[Authorize(Roles = "Moderator,Administrator")]
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
        [Authorize]
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
                await LogToServiceAsync("Information", "update-profile", $"User {request.Username} profile updated");
                return Ok(user);
            }
            catch (AuthenticationException ex)
            {
                await LogToServiceAsync("Error", "user-not-found", $"User {request.Username} not found");
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT api/auth/user/change_role
        [HttpPut("user/change_role")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<UserResponse>> ChangeRole(ChangeRoleRequest request)
        {
            try
            {
                var result = await _authService.ChangeRoleAsync(request);
                await LogToServiceAsync("Information", "change-role", $"User {request.Id} role changed to {request.Role}");
                return Ok(result);
            }
            catch (InvalidDataException ex)
            {
                await LogToServiceAsync("Error", "request-reading-fail", $"Change role request for user {request.Id} reading failed");
                return BadRequest(new { message = ex.Message });
            }
            catch (NullReferenceException ex)
            {
                await LogToServiceAsync("Error", "user-not-found", $"User {request.Id} not found");
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT api/auth/user/set_active_status
        [HttpPut("user/set_active_status")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<UserResponse>> SetActiveStatus([FromBody] SetActiveStatusRequest request)
        {
            try
            {
                var user = await _authService.SetActiveStatusAsync(request.Id, request.IsActive);
                var action = request.IsActive ? "activated" : "deactivated";
                await LogToServiceAsync("Information", $"user-{action}", $"User {user.Username} (ID: {user.Id}) {action}");
                return Ok(user);
            }
            catch (NullReferenceException ex)
            {
                await LogToServiceAsync("Error", "user-not-found", "User not found");
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}