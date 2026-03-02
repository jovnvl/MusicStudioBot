using BookingService.DTO;
using BookingService.Models.Entities;

namespace BookingService.Services
{
    public interface IRoomService
    {
        Task<Room?> GetRoomByIdAsync(int id, CancellationToken ct);
        Task<Room?> GetRoomByNameAsync(string name, CancellationToken ct);
        Task<List<Room>> GetRoomsByCategoryIdAsync(int id, CancellationToken ct);
        Task<Room?> CreateRoomAsync(RoomDto createRoomDto, CancellationToken ct);
        Task<bool> DeleteRoomAsync(int id, CancellationToken ct);
        Task<IReadOnlyList<Room>>GetAllRoomsAsync(CancellationToken ct);
        Task<bool> ExistsRoomAsync(int id, CancellationToken ct);
    }
}
