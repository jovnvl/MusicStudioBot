using IdentityService.Models.DTOs;

namespace IdentityService.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<UserResponse?> GetUserByIdAsync(Guid userId);
        Task<UserResponse?> GetUserByTelegramIdAsync(long telegramId);
        Task<UserResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
    }
}
