using BookingService.Models.DTO;
using BookingService.Services;
using Microsoft.AspNetCore.Mvc;


namespace BookingService.Controllers
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

        // GET api/auth/user/{id}
        [HttpGet("user/{id:guid}")]
        public async Task<ActionResult<UserDto>> GetUserById(Guid id, CancellationToken ct = default)
        {
            try
            {
                var user = await _authService.GetUserByIdAsync(id, ct);
                if (user == null) return NotFound();
                return Ok(user);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}