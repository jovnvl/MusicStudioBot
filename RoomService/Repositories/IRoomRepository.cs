using RoomService.Models.Entities;

namespace RoomService.Repositories
{
    public interface IRoomRepository
    {
        Task<IReadOnlyList<Room>> GetAllRoomsAsync(CancellationToken ct);
        Task<Room?> GetRoomAsync(int id, CancellationToken ct);
        Task AddRoomAsync(Room room, CancellationToken ct);
        Task<bool> RemoveRoomAsync(int id, CancellationToken ct);
    }
}
