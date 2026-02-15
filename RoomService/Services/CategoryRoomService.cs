using RoomService.DTO;
using RoomService.Models.Entities;
using RoomService.Repositories;
using System.Xml.Linq;

namespace RoomService.Services
{
    public class CategoryRoomService : ICategoryRoomService
    {
        private readonly ICategoryRoomRepository _categoryRoomRepository;
        private readonly IRoomsService _roomsService;

        public CategoryRoomService(ICategoryRoomRepository categoryRoomRepository, IRoomsService roomsService)
        {
            _categoryRoomRepository = categoryRoomRepository;
            _roomsService = roomsService;
        }
        public async Task<CategoryRoom?> CreateCategoryRoomAsync(CreateCategoryRoomDto createCategoryRoomDto, CancellationToken ct)
        {
            var result = await GetCategoryRoomByNameAsync(createCategoryRoomDto.Name, ct);
            if (result != null)
                return null;

            return await _categoryRoomRepository.AddGategoryRoomAsync(createCategoryRoomDto, ct);
        }

        public async Task<bool> DeleteCategoryRoomAsync(int id, CancellationToken ct)
        {
            
            var roomList = await _roomsService.GetRoomsByCategoryIdAsync(id, ct);
            if (roomList.Count > 0)
            {
                return false;
            }
            return await _categoryRoomRepository.RemoveCategoryRoomAsync(id, ct);
        }

        public async Task<bool> ExistsCategoryRoomAsync(int id, CancellationToken ct)
        {
            return await GetCategoryRoomByIdAsync(id, ct) != null;
        }

        public async Task<IReadOnlyList<CategoryRoom>> GetAllCategoryRoomAsync(CancellationToken ct)
        {
            return await _categoryRoomRepository.GetAllCategoryRoomsAsync(ct);
        }

        public async Task<CategoryRoom?> GetCategoryRoomByIdAsync(int id, CancellationToken ct)
        {
            var result = await _categoryRoomRepository.GetAllCategoryRoomsAsync(ct);
            return result.Where(x => x.Id == id).FirstOrDefault();
        }

        public async Task<CategoryRoom?> GetCategoryRoomByNameAsync(string name, CancellationToken ct)
        {
            var result = await _categoryRoomRepository.GetAllCategoryRoomsAsync(ct);
            return result.Where(x => x.Name == name).FirstOrDefault();
        }
    }
}
