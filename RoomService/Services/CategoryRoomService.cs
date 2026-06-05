using RoomService.Data;
using RoomService.DTO;
using RoomService.Models.Entities;
using RoomService.Repositories;
using System.Linq.Expressions;
using System.Xml.Linq;

namespace RoomService.Services
{
    public class CategoryRoomService : ICategoryRoomService
    {
        private readonly ICategoryRoomRepository _categoryRoomRepository;
        private readonly IRoomRepository _roomRepository;
        private readonly IOutboundMessagesService _outboundMessagesService;
        private readonly DataContext _dataContext;

        public CategoryRoomService(ICategoryRoomRepository categoryRoomRepository, IRoomRepository roomsRepository, IOutboundMessagesService outboundMessagesService, DataContext dataContext)
        {
            _categoryRoomRepository = categoryRoomRepository;
            _roomRepository = roomsRepository;
            _outboundMessagesService = outboundMessagesService;
            _dataContext = dataContext;

        }
        public async Task<CategoryRoom?> CreateCategoryRoomAsync(CreateCategoryRoomDto createCategoryRoomDto, CancellationToken ct)
        {
            var result = await GetCategoryRoomByNameAsync(createCategoryRoomDto.Name, ct);
            if (result != null)
            {
                await _outboundMessagesService.CreateOutboundMessageToLogAsync(LogLevel.Warning, "Не удалось создать категорию", "add-category", true, ct);
                return null;
            }

            await using var transaction = await _dataContext.Database.BeginTransactionAsync(ct);

            try
            {
                var createdCategory = await _categoryRoomRepository.AddGategoryRoomAsync(createCategoryRoomDto, false, ct);
                await _outboundMessagesService.CreateOutboundMessageToLogAsync(LogLevel.Information, $"Создана категория c названием {createdCategory.Name}", "add-category", false, ct);
                await _dataContext.SaveChangesAsync();
                await transaction.CommitAsync(ct);
                return createdCategory;
            }
            catch 
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<bool> DeleteCategoryRoomAsync(int id, CancellationToken ct)
        {

            var result = await _roomRepository.GetAllRoomsAsync(ct);
            var roomList = result.Where(x => x.CategoryRoomId == id).ToList();

            if (roomList.Count > 0)
            {
                await _outboundMessagesService.CreateOutboundMessageToLogAsync(LogLevel.Warning, "Не удалось удалить категорию", "delete-category", true, ct);
                return false;
            }

            await using var transaction = await _dataContext.Database.BeginTransactionAsync(ct);

            try 
            {
                var isRemoved = await _categoryRoomRepository.RemoveCategoryRoomAsync(id, false, ct);

                if (isRemoved)
                {
                    await _outboundMessagesService.CreateOutboundMessageToLogAsync(LogLevel.Information, $"Удалена категория с ID {id}", "delete-category", false, ct);
                }
                else
                {
                    await _outboundMessagesService.CreateOutboundMessageToLogAsync(LogLevel.Warning, "Не удалось удалить категорию", "delete-category", false, ct);
                }

                await _dataContext.SaveChangesAsync();
                await transaction.CommitAsync(ct);
                return isRemoved;
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<bool> ExistsCategoryRoomAsync(int id, CancellationToken ct)
        {
            return await GetCategoryRoomByIdAsync(id, ct) != null;
        }

        public async Task<IReadOnlyList<CategoryRoom>> GetAllCategoryRoomAsync(CancellationToken ct)
        {
            var categories = await _categoryRoomRepository.GetAllCategoryRoomsAsync(ct);
            await _outboundMessagesService.CreateOutboundMessageToLogAsync(LogLevel.Information, "Успешно получен список всех категорий", "get-categories", true, ct);

            return categories;
        }

        public async Task<CategoryRoom?> GetCategoryRoomByIdAsync(int id, CancellationToken ct)
        {

            var result = await _categoryRoomRepository.GetAllCategoryRoomsAsync(ct);
            var room = result.Where(x => x.Id == id).FirstOrDefault();
            if (room == null)
            {
                await _outboundMessagesService.CreateOutboundMessageToLogAsync(LogLevel.Warning, $"Категория с ID {id} не найдена", "get-category", true, ct);
                return null;
            }
            await _outboundMessagesService.CreateOutboundMessageToLogAsync(LogLevel.Information, $"Получена категория с ID {id}", "get-category", true, ct);

            return room;
        }

        public async Task<CategoryRoom?> GetCategoryRoomByNameAsync(string name, CancellationToken ct)
        {
            var result = await _categoryRoomRepository.GetAllCategoryRoomsAsync(ct);
            return result.Where(x => x.Name == name).FirstOrDefault();
        }
    }
}
