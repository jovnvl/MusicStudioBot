using Microsoft.EntityFrameworkCore.Storage;
using RoomService.DTO;
using RoomService.Models.Entities;

namespace RoomService.Repositories
{
    public interface ICategoryRoomRepository
    {
        Task<IReadOnlyList<CategoryRoom>> GetAllCategoryRoomsAsync(CancellationToken ct);
        Task<CategoryRoom> AddGategoryRoomAsync(CreateCategoryRoomDto createCategoryRoomDto, bool saveChanges, CancellationToken ct);
        Task<bool> RemoveCategoryRoomAsync(int id, bool saveChanges, CancellationToken ct);
    }
}
