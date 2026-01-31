using RoomService.Models.Entities;

namespace RoomService.Repositories
{
    public class InMemoryRoomRepository : IRoomRepository
    {
        private List<Room> roomList;
        public InMemoryRoomRepository()
        {
            roomList = new List<Room>();
        }
        public async Task<Room> GetRoom()
        {
            throw new NotImplementedException();
        }
        public async Task AddRoom(Room room)
        {
            throw new NotImplementedException();
        }
        public async Task RemoveRoom(Room room)
        {
            throw new NotImplementedException();
        }
    }
}
