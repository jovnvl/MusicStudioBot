using IdentityService.Data;
using IdentityService.Models.DTOs;
using IdentityService.Models.Entities;
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
        private readonly IRefreshTokenService _refreshTokenService;
        public AuthService(
            IdentityDbContext context, 
            IConfiguration configuration,
            IRefreshTokenService refreshTokenService)
        {
            _context = context;
            _configuration = configuration;
            _refreshTokenService = refreshTokenService;
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

        public async Task<UserResponse> ChangeRoleAsync(ChangeRoleRequest request)
        {
            var user = await _context.Users.FindAsync(request.Id);
            if (user == null)
            {
                throw new NullReferenceException("Пользователь не найден.");
            }

            if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var requestRole))
            {
                throw new InvalidDataException("Ошибка чтения запроса.");
            }
            user.Role = requestRole;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
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
                throw new NullReferenceException("Пользователь не найден.");
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                throw new AuthenticationException("Неверный пароль.");
            }

            if (!user.IsActive)
            {           
                throw new AccessViolationException("Доступ закрыт.");
            }

            var token = GenerateJwtToken(user);
            var refreshToken = await _refreshTokenService.GenerateRefreshTokenAsync(user.Id);
            return new AuthResponse { Token = token, RefreshToken = refreshToken.Token, UserId = user.Id, Username = user.Username, Role = user.Role.ToString() };
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.TelegramId == request.TelegramId))
            {
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
            var refreshToken = await _refreshTokenService.GenerateRefreshTokenAsync(user.Id);
            return new AuthResponse{Token = token, RefreshToken = refreshToken.Token, UserId = user.Id, Username = user.Username, Role = user.Role.ToString() };
        }

        public async Task<UserResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            { 
                throw new AuthenticationException("Пользователь не найден.");
            }

            user.Username = request.Username ?? user.Username;
            user.FirstName = request.FirstName ?? user.FirstName;
            user.LastName = request.LastName ?? user.LastName;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return MapToResponse(user); 
        }

        public async Task<UserResponse> SetActiveStatusAsync(Guid userId, bool isActive)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new NullReferenceException("Пользователь не найден.");
            }

            user.IsActive = isActive;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
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
            if (key == null)
            {
                throw new NullReferenceException("Не удалось получить Secretkey");
            }

            // 3. Создаем подпись (алгоритм HMAC-SHA256)
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            if (credentials == null)
            {
                throw new NullReferenceException("Не удалось создать подпись (алгоритм HMAC-SHA256)");
            }

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

        public async Task<AuthResponse> RefreshTokenAsync(string refreshTokenString)
        {
            var refreshToken = await _refreshTokenService.GetRefreshTokenAsync(refreshTokenString);

            if (refreshToken == null)
            {
                throw new NullReferenceException("Refresh token не найден");
            }
            if (refreshToken.IsExpired)
            { 
                throw new SecurityTokenException("Недействительный refresh token"); 
            }

            var user = await _context.Users.FindAsync(refreshToken.UserId);
            if (user == null)
            {
                throw new NullReferenceException("Пользователь не найден");
            }
            if (!user.IsActive)
            {
                throw new AccessViolationException("Доступ закрыт.");
            }

            var newAccessToken = GenerateJwtToken(user);
            var newRefreshToken = await _refreshTokenService.GenerateRefreshTokenAsync(user.Id);

            await _refreshTokenService.RevokeRefreshTokenAsync(refreshTokenString);

            return new AuthResponse
            {
                Token = newAccessToken,
                RefreshToken = newRefreshToken.Token,
                UserId = user.Id,
                Username = user.Username,
                Role = user.Role.ToString()
            };
        }

        public async Task RevokeTokenAsync(string refreshToken)
        {
            await _refreshTokenService.RevokeRefreshTokenAsync(refreshToken);
        }

        public async Task<AuthResponse> TelegramLoginAsync(long telegramId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.TelegramId == telegramId);
            
            if (user == null)
            {
                throw new NullReferenceException("Пользователь не найден");
            }
            if (!user.IsActive)
            {
                throw new AccessViolationException("Доступ закрыт.");
            }

            var token = GenerateJwtToken(user);
            var refreshToken = await _refreshTokenService.GenerateRefreshTokenAsync(user.Id);

            return new AuthResponse
            {
                Token = token,
                RefreshToken = refreshToken.Token,
                UserId = user.Id,
                Username = user.Username,
                Role = user.Role.ToString()
            };
        }
    }
}
