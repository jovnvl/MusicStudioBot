using BookingService.Models.DTO;

namespace BookingService.Services
{
    public interface IAuthService
    {
        Task<UserDto?> GetUserByIdAsync(Guid userId, CancellationToken ct);
        Task<bool> ExistsUserAsync(Guid id, CancellationToken ct);
    }
}
