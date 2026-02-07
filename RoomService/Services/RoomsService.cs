using RoomService.Models.Entities;
using RoomService.Repositories;

namespace RoomService.Services
{
    public class RoomsService : IRoomsService
    {
        IRoomRepository _roomRepository;

        public RoomsService(IRoomRepository roomRepository)
        {
            _roomRepository = roomRepository;
        }

        public async Task CreateRoomAsync(Room room, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> DeleteRoomAsync(int id, CancellationToken ct)
        {
            return await _roomRepository.RemoveRoomAsync(id, ct);
        }

        public async Task<IReadOnlyList<Room>> GetAllRoomsAsync(CancellationToken ct)
        {
            return await _roomRepository.GetAllRoomsAsync(ct);
        }

        public async Task<Room?> GetRoomAsync(int id, CancellationToken ct)
        {
            return await _roomRepository.GetRoomAsync(id, ct);
        }

        public async Task<bool> ExistsRoomAsync(int id, CancellationToken ct)
        {
            return await GetRoomAsync(id, ct) != null;
        }
    }
}
