using BookingService.DTO;
using BookingService.Models.Entities;
using BookingService.Repositories;

namespace BookingService.Services
{
    public class RoomService : IRoomService
    {
        private readonly IRoomRepository _roomRepository;
        private readonly ICategoryRoomService _categoryRoomService;

        public RoomService(IRoomRepository roomRepository, ICategoryRoomService categoryRoomService)
        {
            _roomRepository = roomRepository;
            _categoryRoomService = categoryRoomService;
        }

        public async Task<Room?> CreateRoomAsync(RoomDto createRoomDto, CancellationToken ct)
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
            var result = await _roomRepository.GetRoomByIdAsync(id, ct);
            return result;
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
        public async Task<bool> ExistsRoomAsync(int id, CancellationToken ct)
        {
            return await GetRoomByIdAsync(id, ct) != null;
        }
        public async Task<bool> UpdateRoomAsync(UpdateRoomDto updateRoomDto, CancellationToken ct)
        {
            var result = await _roomRepository.UpdateRoomAsync(updateRoomDto, ct);
            return result;
        }
    }
}
