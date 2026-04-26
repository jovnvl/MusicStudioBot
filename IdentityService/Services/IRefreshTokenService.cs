using IdentityService.Models.Entities;

namespace IdentityService.Services
{
    public interface IRefreshTokenService
    {
        Task<RefreshToken> GenerateRefreshTokenAsync(Guid userId);
        Task<RefreshToken?> GetRefreshTokenAsync(string token);
        Task SaveRefreshTokenAsync(RefreshToken refreshToken);
        Task RevokeRefreshTokenAsync(string token);
    }
}
