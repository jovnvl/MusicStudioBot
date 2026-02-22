using RoomService.DTO;
using RoomService.Models.Entities;

namespace RoomService.Repositories
{
    public class InMemoryCategoryRoomRepository : ICategoryRoomRepository
    {

        static List<CategoryRoom> _categoryRoomList;
        static InMemoryCategoryRoomRepository()
        {
            _categoryRoomList = new List<CategoryRoom>();
        }
        public async Task<CategoryRoom> AddGategoryRoomAsync(CreateCategoryRoomDto createCategoryRoomDto, CancellationToken ct)
        {
            var categoryRoom = new CategoryRoom
            {
                Id = _categoryRoomList.Count + 1,
                Name = createCategoryRoomDto.Name,
                Description = createCategoryRoomDto.Description
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
