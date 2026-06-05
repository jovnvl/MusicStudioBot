using RoomService.DTO;
using RoomService.Models.Entities;

namespace RoomService.Services
{
    public interface IRoomsService
    {
        Task<Room?> GetRoomByIdAsync(int id, CancellationToken ct);
        Task<Room?> GetRoomByNameAsync(string name, CancellationToken ct);
        Task<List<Room>> GetRoomsByCategoryIdAsync(int id, CancellationToken ct);
        Task<Room?> CreateRoomAsync(CreateRoomDto createRoomDto, CancellationToken ct);
        Task<bool> DeleteRoomAsync(int id, CancellationToken ct);
        Task<IReadOnlyList<Room>>GetAllRoomsAsync(CancellationToken ct);
        Task<bool> UpdateRoomAsync(UpdateRoomDto updateRoomDto, CancellationToken ct);
    }
}
