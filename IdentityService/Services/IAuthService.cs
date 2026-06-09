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
        Task<IReadOnlyList<UserResponse>> GetAllUsersAsync();
        Task<UserResponse> ChangeRoleAsync(ChangeRoleRequest request);
        Task<UserResponse> SetActiveStatusAsync(Guid userId, bool isActive);
        Task<AuthResponse> RefreshTokenAsync(string refreshToken);
        Task RevokeTokenAsync(string refreshToken);
        Task<AuthResponse> TelegramLoginAsync(long telegramId);
        Task<AuthResponse> AdminLoginAsync(AdminLoginRequest request);
    }
}
