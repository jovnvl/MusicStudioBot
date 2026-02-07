using RoomService.Models.Entities;

namespace RoomService.Services
{
    public interface IRoomsService
    {
        Task<Room?> GetRoomAsync(int id, CancellationToken ct);
        Task CreateRoomAsync(Room room, CancellationToken ct);
        Task<bool> DeleteRoomAsync(int id, CancellationToken ct);
        Task<IReadOnlyList<Room>>GetAllRoomsAsync(CancellationToken ct);
        Task<bool>ExistsRoomAsync(int id, CancellationToken ct);
    }
}
