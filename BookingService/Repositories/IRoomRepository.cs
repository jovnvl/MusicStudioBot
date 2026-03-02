using BookingService.DTO;
using BookingService.Models.Entities;

namespace BookingService.Repositories
{
    public interface IRoomRepository
    {
        Task<IReadOnlyList<Room>> GetAllRoomsAsync(CancellationToken ct);
        Task<Room?> GetRoomByIdAsync(int Id, CancellationToken ct);
        Task<Room> AddRoomAsync(RoomDto createRoomDto, CancellationToken ct);
        Task<bool> RemoveRoomAsync(int id, CancellationToken ct);
        Task<bool> UpdateRoomAsync(UpdateRoomDto updateRoomDto, CancellationToken ct);
    }
}
