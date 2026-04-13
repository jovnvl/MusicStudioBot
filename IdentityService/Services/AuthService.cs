using IdentityService.Data;
using IdentityService.DTO;
using IdentityService.Models.DTOs;
using IdentityService.Models.Entities;
using IdentityService.Services.RabbitMQ;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Authentication;
using System.Security.Claims;
using System.Text;

namespace IdentityService.Services
{
    public class AuthService : IAuthService
    {
        private readonly IdentityDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IRabbitMQPublisher _rabbitMQPublisher;

        public AuthService(IdentityDbContext context, IConfiguration configuration, IRabbitMQPublisher RabbitMQPublisher)
        {
            _context = context;
            _configuration = configuration;
            _rabbitMQPublisher = RabbitMQPublisher;
        }

        private static UserResponse MapToResponse(User user)
        {
            return new UserResponse
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

        private async Task LogToServiceAsync(string level, string eventType, string message)
        {
            await _rabbitMQPublisher.PublishAsync("logging_service_queue", new LogEventDto(level, eventType, message));
        }

        public async Task<UserResponse> ChangeRoleAsync(ChangeRoleRequest request)
        {
            var user = await _context.Users.FindAsync(request.Id);
            if (user == null)
            {
                await LogToServiceAsync("Error", "user-not-found", "User not found");
                throw new InvalidOperationException("Пользователь не найден.");
            }

            if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var requestRole))
            {
                await LogToServiceAsync("Error", "role-request-reading-failed", "Request reading failed");
                throw new InvalidOperationException("Ошибка чтения запроса.");
            }
            user.Role = requestRole;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await LogToServiceAsync("Information", "change-role", $"User {user.Username} role changed to {requestRole}");
            return MapToResponse(user);
        }

        public async Task<IReadOnlyList<UserResponse>> GetAllUsersAsync()
        {
            var users = await _context.Users.ToListAsync(); 
            return users.Select(u => MapToResponse(u)).ToList();
        }

        public async Task<UserResponse?> GetUserByIdAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            return user == null ? null : MapToResponse(user);
        }

        public async Task<UserResponse?> GetUserByTelegramIdAsync(long telegramId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.TelegramId == telegramId);
            return user == null ? null : MapToResponse(user);
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.TelegramId == request.TelegramId);
            
            if (user == null)
            {
                await LogToServiceAsync("Error", "user-not-found", $"User not found");
                throw new AuthenticationException("Пользователь не найден.");
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                await LogToServiceAsync("Error", "incorrect-password", $"Password is incorrect");
                throw new AuthenticationException("Неверный пароль.");
            }

            if (!user.IsActive)
            {
                await LogToServiceAsync("Error", "profile-deactivated", $"User profile is inactive");
                throw new AuthenticationException("Доступ закрыт.");
            }

            var token = GenerateJwtToken(user);
            await LogToServiceAsync("Information", "login", $"Login of User {user.Username} is successfull");
            return new AuthResponse { Token = token, UserId = user.Id, Username = user.Username, Role = user.Role.ToString() };
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.TelegramId == request.TelegramId))
            {
                await LogToServiceAsync("Error", "user-already-registered", $"User already has a profile");
                throw new InvalidOperationException("Пользователь уже зарегистрирован.");
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                TelegramId = request.TelegramId,
                Username = request.Username, 
                FirstName = request.FirstName,
                LastName = request.LastName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = UserRole.Student,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true,
            };

            _context.Users.Add(user); 
            await _context.SaveChangesAsync();
            var token = GenerateJwtToken(user);
            await LogToServiceAsync("Information", "registration", $"Successfull registration");
            return new AuthResponse{Token = token, UserId = user.Id, Username = user.Username, Role = user.Role.ToString() };
        }

        public async Task<UserResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                await LogToServiceAsync("Error", "user-not-found", $"User not found");
                throw new AuthenticationException("Пользователь не найден.");
            }

            user.Username = request.Username ?? user.Username;
            user.FirstName = request.FirstName ?? user.FirstName;
            user.LastName = request.LastName ?? user.LastName;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await LogToServiceAsync("Information", "update-profile", $"User profile updated");
            return MapToResponse(user); 
        }

        private string GenerateJwtToken(User user)
        {
            // 1. Создаем claims (данные пользователя)
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("telegram_id", user.TelegramId.ToString())
            };

            // 2. Получаем SecretKey из конфигурации и создаем ключ
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]!));

            // 3. Создаем подпись (алгоритм HMAC-SHA256)
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // 4. Настраиваем токен
            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    double.Parse(_configuration["JwtSettings:ExpirationMinutes"]!)
                ),
                signingCredentials: credentials
            );

            // 5. Генерируем строку токена
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
