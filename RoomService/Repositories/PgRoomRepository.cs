using Microsoft.EntityFrameworkCore;
using RoomService.Data;
using RoomService.DTO;
using RoomService.Models.Entities;

namespace RoomService.Repositories
{
    public class PgRoomRepository : IRoomRepository
    {
        private readonly DataContext _dataContext;

        public PgRoomRepository(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task<Room> AddRoomAsync(CreateRoomDto createRoomDto, CancellationToken ct)
        {
            var room = new Room
            {
                Name = createRoomDto.Name,
                Description = createRoomDto.Description,
                Photo = createRoomDto.Photo,
                CreationDate = DateOnly.FromDateTime(DateTime.UtcNow),
                CategoryRoomId = createRoomDto.CategoryRoomId,
                Status = createRoomDto.Status
            };
            await _dataContext.Rooms.AddAsync(room, ct);
            await _dataContext.SaveChangesAsync(ct);
            return room;
        }

        public async Task<IReadOnlyList<Room>> GetAllRoomsAsync(CancellationToken ct)
        {
            var _rooms = await _dataContext.Rooms.ToListAsync(ct);
            return _rooms.AsReadOnly();
        }

        public async Task<bool> RemoveRoomAsync(int id, CancellationToken ct)
        {
            var room = await _dataContext.Rooms.FindAsync(id, ct);

            if (room == null)
                return false;

            _dataContext.Rooms.Remove(room);
            await _dataContext.SaveChangesAsync(ct);

            return true;
        }
    }
}
