using Microsoft.EntityFrameworkCore;
using BookingService.Data;
using BookingService.DTO;
using BookingService.Models.Entities;

namespace BookingService.Repositories
{
    public class RoomRepository : IRoomRepository
    {
        private readonly DataContext _dataContext;

        public RoomRepository(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task<Room> AddRoomAsync(RoomDto createRoomDto, CancellationToken ct)
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
