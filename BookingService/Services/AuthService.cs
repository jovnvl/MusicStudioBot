using BookingService.Data;
using BookingService.Models.DTO;
using BookingService.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Authentication;
using System.Security.Claims;
using System.Text;

namespace BookingService.Services
{
    public class AuthService : IAuthService
    {
        private readonly DataContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(DataContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<UserDto?> GetUserByIdAsync(Guid userId, CancellationToken ct)
        {
            var user = await _context.Users.FindAsync(userId, ct);
            if (user == null) 
                return null; 
            return new UserDto
            {
                Id = user.Id,
                TelegramId = user.TelegramId,
                Username = user.Username,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role.ToString(),
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
            };
        }
        public async Task<bool> ExistsUserAsync(Guid id, CancellationToken ct)
        {
            return await GetUserByIdAsync(id, ct) != null;
        }

    }
}
