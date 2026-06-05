using RoomService.Data;
using RoomService.DTO;
using RoomService.Models.Entities;
using RoomService.Repositories;

namespace RoomService.Services
{
    public class RoomsService : IRoomsService
    {
        private readonly IRoomRepository _roomRepository;
        private readonly ICategoryRoomService _categoryRoomService;
        private readonly IOutboundMessagesService _outboundMessagesService;
        private readonly DataContext _dataContext;


        public RoomsService(IRoomRepository roomRepository, ICategoryRoomService categoryRoomService, IOutboundMessagesService outboundMessagesService, DataContext dataContext)
        {
            _roomRepository = roomRepository;
            _categoryRoomService = categoryRoomService;
            _outboundMessagesService = outboundMessagesService;
            _dataContext = dataContext;
        }

        public async Task<Room?> CreateRoomAsync(CreateRoomDto createRoomDto, CancellationToken ct)
        {
            var categoryExists = await _categoryRoomService.ExistsCategoryRoomAsync(createRoomDto.CategoryRoomId, ct);
            if (!categoryExists)
            {
                await _outboundMessagesService.CreateOutboundMessageToLogAsync(LogLevel.Warning, "Не удалось создать комнату", "post-rooms", true, ct);
                return null;
            }

            var room = await GetRoomByNameAsync(createRoomDto.Name, ct);
            if (room != null)
            {
                await _outboundMessagesService.CreateOutboundMessageToLogAsync(LogLevel.Warning, "Не удалось создать комнату", "post-rooms", true, ct);
                return null;
            }

            await using var transaction = await _dataContext.Database.BeginTransactionAsync(ct);
            try
            {
                var createdRoom =  await _roomRepository.AddRoomAsync(createRoomDto, false, ct);
                await _outboundMessagesService.CreateOutboundMessageToLogAsync(LogLevel.Information, $"Создана комната с названием {createdRoom.Name }", "post-rooms", false, ct);
                await _dataContext.SaveChangesAsync();
                await transaction.CommitAsync(ct);
                return createdRoom;
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<bool> DeleteRoomAsync(int id, CancellationToken ct)
        {
            await using var transaction = await _dataContext.Database.BeginTransactionAsync(ct);
            try
            {
               var isDeleted = await _roomRepository.RemoveRoomAsync(id, false, ct);
                if (isDeleted)
                {
                    await _outboundMessagesService.CreateOutboundMessageToLogAsync(LogLevel.Information, $"Комната с ID {id} удалена", "delete-room", false, ct);
                }
                else
                {
                    await _outboundMessagesService.CreateOutboundMessageToLogAsync(LogLevel.Warning, $"Не удалось удалить комнату с ID {id}", "delete-room", false, ct);
                }
                await _dataContext.SaveChangesAsync();
                await transaction.CommitAsync(ct);

                return isDeleted;
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<IReadOnlyList<Room>> GetAllRoomsAsync(CancellationToken ct)
        {
            var rooms = await _roomRepository.GetAllRoomsAsync(ct);
            await _outboundMessagesService.CreateOutboundMessageToLogAsync(LogLevel.Information, "Успешно запрошен список всех комнат", "get-rooms", true, ct);
            return rooms;
        }

        public async Task<Room?> GetRoomByIdAsync(int id, CancellationToken ct)
        {
            var result = await _roomRepository.GetRoomByIdAsync(id, ct);
            if (result == null)
            {
                await _outboundMessagesService.CreateOutboundMessageToLogAsync(LogLevel.Warning, $"Комната с ID {id} не найдена", "get-room", true, ct);
                return null;
            }
            await _outboundMessagesService.CreateOutboundMessageToLogAsync(LogLevel.Information, $"Комната с ID {id} найдена", "get-room", true, ct);
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

        public async Task<bool> UpdateRoomAsync(UpdateRoomDto updateRoomDto, CancellationToken ct)
        {
            await using var transaction = await _dataContext.Database.BeginTransactionAsync(ct);
            try
            {
                var isUpdated = await _roomRepository.UpdateRoomAsync(updateRoomDto, false, ct);
                if (isUpdated)
                {
                    await _outboundMessagesService.CreateOutboundMessageToLogAsync(LogLevel.Information, $"Комната с ID {updateRoomDto.Id} обновлена", "update-room", false, ct);
                }
                else
                {
                    await _outboundMessagesService.CreateOutboundMessageToLogAsync(LogLevel.Warning, "Не удалось обновить комнату", "update-room", false, ct);
                }

                await _dataContext.SaveChangesAsync();
                await transaction.CommitAsync(ct);

                return isUpdated;
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }

        }
    }
}
