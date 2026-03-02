using BookingService.Data;
using BookingService.DTO;
using BookingService.Models.Entities;
using System.Diagnostics.Eventing.Reader;
using System.Reflection.Metadata.Ecma335;

namespace BookingService.Repositories
{
    public class MemoryRoomRepository : IRoomRepository
    {
        static List<Room> _roomList;
        static MemoryRoomRepository()
        {
            _roomList = new List<Room>();
        }
        public async Task<Room> AddRoomAsync(RoomDto createRoomDto, CancellationToken ct)
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
            var room = await GetRoomByIdAsync(id, ct);
            if (room != null)
            {
                if (_roomList.Remove(room))
                    return true;
                else
                    return false;
            }

            return false;
        }
        public async Task<IReadOnlyList<Room>> GetAllRoomsAsync(CancellationToken ct)
        {
            return _roomList.AsReadOnly();
        }

        public async Task<Room?> GetRoomByIdAsync(int Id, CancellationToken ct)
        {
            return _roomList.FirstOrDefault(x => x.Id == Id);

        }

        public async Task<bool> UpdateRoomAsync(UpdateRoomDto updateRoomDto, CancellationToken ct)
        {
            var room = await GetRoomByIdAsync(updateRoomDto.Id, ct);

            if (room == null)
                return false;

            room.Status = updateRoomDto.Status;
            return true;
        }
    }
}
