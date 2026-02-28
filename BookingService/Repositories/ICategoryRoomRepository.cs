using BookingService.DTO;
using BookingService.Models.Entities;

namespace BookingService.Repositories
{
    public interface ICategoryRoomRepository
    {
        Task<IReadOnlyList<CategoryRoom>> GetAllCategoryRoomsAsync(CancellationToken ct);
        Task<CategoryRoom> AddGategoryRoomAsync(CategoryRoomDto createCategoryRoomDto, CancellationToken ct);
        Task<bool> RemoveCategoryRoomAsync(int id, CancellationToken ct);
    }
}
