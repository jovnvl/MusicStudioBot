using Microsoft.EntityFrameworkCore;
using RoomService.Data;
using RoomService.DTO;
using RoomService.Models.Entities;

namespace RoomService.Repositories
{
    public class PgCategoryRoomRepository : ICategoryRoomRepository
    {
        private readonly DataContext _dataContext;

        public PgCategoryRoomRepository(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task<CategoryRoom> AddGategoryRoomAsync(CreateCategoryRoomDto createCategoryRoomDto, CancellationToken ct)
        {
            var categoryRoom = new CategoryRoom {
                Name = createCategoryRoomDto.Name, 
                Description = createCategoryRoomDto.Description
                };
            await _dataContext.CategoryRooms.AddAsync(categoryRoom, ct);
            await _dataContext.SaveChangesAsync(ct);
            return categoryRoom;
        }

        public async Task<IReadOnlyList<CategoryRoom>> GetAllCategoryRoomsAsync(CancellationToken ct)
        {
            var _categoryRooms = await _dataContext.CategoryRooms.ToListAsync(ct);
            return _categoryRooms.AsReadOnly();
        }

        public async Task<bool> RemoveCategoryRoomAsync(int id, CancellationToken ct)
        {
            var categoryRoom = await _dataContext.CategoryRooms.FindAsync(id, ct);

            if (categoryRoom == null)
                return false;

            _dataContext.CategoryRooms.Remove(categoryRoom);
            await _dataContext.SaveChangesAsync(ct);

            return true;
        }
    }
}
