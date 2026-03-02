using RoomService.DTO;
using RoomService.Models.Entities;

namespace RoomService.Repositories
{
    public interface IRoomRepository
    {
        Task<IReadOnlyList<Room>> GetAllRoomsAsync(CancellationToken ct);
        Task<Room?> GetRoomByIdAsync(int Id, CancellationToken ct);
        Task<Room> AddRoomAsync(CreateRoomDto createRoomDto, CancellationToken ct);
        Task<bool> RemoveRoomAsync(int id, CancellationToken ct);
        Task<bool> UpdateRoomAsync(UpdateRoomDto updateRoomDto, CancellationToken ct);
    }
}
