using RoomService.Models.Entities;

namespace RoomService.Services.Interfaces
{
    public interface IRoomService
    {
        Task<Room> GetRoom();
        Task AddRoom(Room room);
        Task RemoveRoom(Room room);
    }
}
