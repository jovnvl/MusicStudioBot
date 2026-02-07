using RoomService.Models.Entities;
using System.Diagnostics.Eventing.Reader;
using System.Reflection.Metadata.Ecma335;

namespace RoomService.Repositories
{
    public class InMemoryRoomRepository : IRoomRepository
    {
        static List<Room> _roomList;
        static InMemoryRoomRepository()
        {
            _roomList = new List<Room>();
            _roomList.Add( new Room(1, "dfg", "desc", null, DateOnly.FromDateTime(DateTime.UtcNow), new CategoryRoomDto(), null));
            _roomList.Add(new Room(2, "zxc", "desc2", null, DateOnly.FromDateTime(DateTime.UtcNow), new CategoryRoomDto(), null));
        }
        public async Task<Room?> GetRoomAsync(int id, CancellationToken ct)
        {
            foreach (var room in _roomList) 
            {
                if (room.Id == id) 
                    return room;
            }
            return null;
        }
        public async Task AddRoomAsync(Room room, CancellationToken ct)
        {
            throw new NotImplementedException();
        }
        public async Task<bool> RemoveRoomAsync(int id, CancellationToken ct)
        {
            foreach (var room in _roomList)
            {
                if (room.Id == id)
                {
                    if (_roomList.Remove(room))
                        return true;
                    else 
                        return false;
                }
            }
            return false;
        }
        public async Task<IReadOnlyList<Room>> GetAllRoomsAsync(CancellationToken ct)
        {
            return _roomList;
        }
    }
}
