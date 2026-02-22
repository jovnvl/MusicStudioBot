using RoomService.DTO;
using RoomService.Models.Entities;
using RoomService.Repositories;

namespace RoomService.Services
{
    public class RoomsService : IRoomsService
    {
        private readonly IRoomRepository _roomRepository;
        private readonly ICategoryRoomService _categoryRoomService;

        public RoomsService(IRoomRepository roomRepository, ICategoryRoomService categoryRoomService)
        {
            _roomRepository = roomRepository;
            _categoryRoomService = categoryRoomService;
        }

        public async Task<Room?> CreateRoomAsync(CreateRoomDto createRoomDto, CancellationToken ct)
        {
            var categoryExists = await _categoryRoomService.ExistsCategoryRoomAsync(createRoomDto.CategoryRoomId, ct);
            if (!categoryExists)
            {
                return null;
            }

            var room = await GetRoomByNameAsync(createRoomDto.Name, ct);
            if (room != null)
            {
                return null;
            }
            return await _roomRepository.AddRoomAsync(createRoomDto, ct);
        }

        public async Task<bool> DeleteRoomAsync(int id, CancellationToken ct)
        {
            return await _roomRepository.RemoveRoomAsync(id, ct);
        }

        public async Task<IReadOnlyList<Room>> GetAllRoomsAsync(CancellationToken ct)
        {
            return await _roomRepository.GetAllRoomsAsync(ct);
        }

        public async Task<Room?> GetRoomByIdAsync(int id, CancellationToken ct)
        {
            var result = await _roomRepository.GetAllRoomsAsync(ct);
            return result.Where(x => x.Id == id).FirstOrDefault();
        }

        public async Task<Room?> GetRoomByNameAsync(string name, CancellationToken ct)
        {
            var result = await _roomRepository.GetAllRoomsAsync(ct);
            return result.Where(x => x.Name == name).FirstOrDefault();
        }

        public async Task<List<Room>> GetRoomsByCategoryIdAsync(int categoryRoomId, CancellationToken ct)
        {
            var result = await _roomRepository.GetAllRoomsAsync(ct);
            return result.Where(x => x.CategoryRoomId == categoryRoomId).ToList();
        }
    }
}
