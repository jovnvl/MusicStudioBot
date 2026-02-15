using RoomService.DTO;
using RoomService.Models.Entities;

namespace RoomService.Repositories
{
    public interface ICategoryRoomRepository
    {
        Task<IReadOnlyList<CategoryRoom>> GetAllCategoryRoomsAsync(CancellationToken ct);
        Task<CategoryRoom> AddGategoryRoomAsync(CreateCategoryRoomDto createCategoryRoomDto, CancellationToken ct);
        Task<bool> RemoveCategoryRoomAsync(int id, CancellationToken ct);
    }
}
