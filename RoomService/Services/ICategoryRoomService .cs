using RoomService.DTO;
using RoomService.Models.Entities;

namespace RoomService.Services
{
    public interface ICategoryRoomService
    {
        Task<CategoryRoom?> GetCategoryRoomByIdAsync(int id, CancellationToken ct);
        Task<CategoryRoom?> GetCategoryRoomByNameAsync(string name, CancellationToken ct);
        Task<CategoryRoom?> CreateCategoryRoomAsync(CreateCategoryRoomDto createCategoryRoomDto, CancellationToken ct);
        Task<bool> DeleteCategoryRoomAsync(int id, CancellationToken ct);
        Task<IReadOnlyList<CategoryRoom>> GetAllCategoryRoomAsync(CancellationToken ct);
        Task<bool> ExistsCategoryRoomAsync(int id, CancellationToken ct);
    }
}
