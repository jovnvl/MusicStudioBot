using RoomService.Models.Entities;

namespace RoomService.Repositories
{
    public interface IRoomRepository
    {
        Task<Room> GetRoom();
        Task AddRoom(Room room);
        Task RemoveRoom(Room room);
    }
}
