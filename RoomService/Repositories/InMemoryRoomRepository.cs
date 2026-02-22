using RoomService.DTO;
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
        }
        public async Task<Room> AddRoomAsync(CreateRoomDto createRoomDto, CancellationToken ct)
        {
            var room = new Room 
            {
                Id = _roomList.Count + 1,
                Name = createRoomDto.Name,
                Description = createRoomDto.Description,
                Photo = createRoomDto.Photo,
                CreationDate = DateOnly.FromDateTime(DateTime.UtcNow),
                CategoryRoomId = createRoomDto.CategoryRoomId,
                Status = createRoomDto.Status,
            };
            _roomList.Add(room);
            return room;
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
            return _roomList.AsReadOnly();
        }
    }
}
