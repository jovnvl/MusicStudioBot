using IdentityService.Models.Entities;
using StackExchange.Redis;
using System.Security.Cryptography;
using System.Text.Json;

namespace IdentityService.Services
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly IDatabase _db;
        private readonly IConfiguration _configuration;

        public RefreshTokenService(IConnectionMultiplexer redis, IConfiguration configuration)
        {
            _db = redis.GetDatabase();
            _configuration = configuration;
        }

        public async Task<RefreshToken> GenerateRefreshTokenAsync(Guid userId)
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);

            var token = Convert.ToBase64String(randomBytes);
            var expirationDays = int.Parse(_configuration["JwtSettings:RefreshTokenExpirationDays"]!);

            var refreshToken = new RefreshToken
            {
                Token = token,
                UserId = userId,
                ExpiresAt = DateTime.UtcNow.AddDays(expirationDays)
            };

            await SaveRefreshTokenAsync(refreshToken);
            return refreshToken;
        }

        public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
        {
            var key = $"refresh_token:{token}";
            var value = await _db.StringGetAsync(key);

            if (value.IsNullOrEmpty)
                return null;

            string json = value!;
            return JsonSerializer.Deserialize<RefreshToken>(json);
        }

        public async Task SaveRefreshTokenAsync(RefreshToken refreshToken)
        {
            var key = $"refresh_token:{refreshToken.Token}";
            var value = JsonSerializer.Serialize(refreshToken);
            var expiry = refreshToken.ExpiresAt - DateTime.UtcNow;

            await _db.StringSetAsync(key, value, expiry);
        }

        public async Task RevokeRefreshTokenAsync(string token)
        {
            await _db.KeyDeleteAsync($"refresh_token:{token}");
        }
    }
}