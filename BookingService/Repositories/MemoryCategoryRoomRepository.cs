using BookingService.DTO;
using BookingService.Models.Entities;

namespace BookingService.Repositories
{
    public class MemoryCategoryRoomRepository : ICategoryRoomRepository
    {

        static List<CategoryRoom> _categoryRoomList;
        static MemoryCategoryRoomRepository()
        {
            _categoryRoomList = new List<CategoryRoom>();
        }
        public async Task<CategoryRoom> AddGategoryRoomAsync(CategoryRoomDto createCategoryRoomDto, CancellationToken ct)
        {
            var categoryRoom = new CategoryRoom(name: createCategoryRoomDto.Name, description: createCategoryRoomDto.Description)
            {
                Id = _categoryRoomList.Count + 1,
            };
            _categoryRoomList.Add(categoryRoom);
            return categoryRoom;
        }

        public async Task<IReadOnlyList<CategoryRoom>> GetAllCategoryRoomsAsync(CancellationToken ct)
        {
            return _categoryRoomList.AsReadOnly();
        }
        public async Task<bool> RemoveCategoryRoomAsync(int id, CancellationToken ct)
        {
            foreach (var categoryRoom in _categoryRoomList)
            {
                if (categoryRoom.Id == id)
                {
                    if (_categoryRoomList.Remove(categoryRoom))
                        return true;
                    else
                        return false;
                }
            }
            return false;
        }
    }
}
