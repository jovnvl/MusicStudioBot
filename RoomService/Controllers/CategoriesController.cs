using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RoomService.DTO;
using RoomService.Models.Entities;
using RoomService.Services;

namespace RoomService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryRoomService _categoryRoomsService;
        private readonly IMessageBrokerService _brokerService;
        public CategoriesController(ICategoryRoomService categoryRoomService, IMessageBrokerService brokerService)
        {
            _categoryRoomsService = categoryRoomService;
            _brokerService = brokerService;
        }

        // GET: api/categories
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<CategoryRoom>>> GetAllCategoriesAsync(CancellationToken ct = default)
        {
            try
            {
                var categoryRooms = await _categoryRoomsService.GetAllCategoryRoomAsync(ct);
                await _brokerService.SendMessageToLogAsync(LogLevel.Information, "Успешно получен список всех категорий", "get-categories", ct);
                return Ok(categoryRooms);
            }
            
            catch (OperationCanceledException)
            {
                return StatusCode(499); 
            }
            
            catch (Exception ex)
            {
                await _brokerService.SendMessageToLogAsync(LogLevel.Critical, ex.Message, "get-categories", ct);
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера" });
            }
        }

        // POST: api/categories
        [HttpPost]
        public async Task<ActionResult<CategoryRoom>> CreateCategoryRoomAsync(CreateCategoryRoomDto categoryRoomDto, CancellationToken ct = default)
        {
            try
            {
                var createdCategory = await _categoryRoomsService.CreateCategoryRoomAsync(categoryRoomDto, ct);

                if (createdCategory == null)
                {
                    var message = "Не удалось создать категорию";
                    await _brokerService.SendMessageToLogAsync(LogLevel.Warning, message, "add-category", ct);
                    return BadRequest(new { Message = message });
                }

                await _brokerService.SendMessageToLogAsync(LogLevel.Information, $"Создана категория с ID {createdCategory.Id}", "add-category", ct);
                return CreatedAtRoute("GetCategoryRoomAsync",new { id = createdCategory.Id }, createdCategory);
            }
            
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            
            catch (Exception ex)
            {
                await _brokerService.SendMessageToLogAsync(LogLevel.Critical, ex.Message, "add-category", ct);
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера" });
            }
        }

        // DELETE: api/categories/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteRoomAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var deleted = await _categoryRoomsService.DeleteCategoryRoomAsync(id, ct);
                if (!deleted)
                {
                    var message = "Не удалось удалить категорию";
                    await _brokerService.SendMessageToLogAsync(LogLevel.Warning, message, "delete-category", ct);
                    return BadRequest(new { Message = message });
                }
                
                await _brokerService.SendMessageToLogAsync(LogLevel.Information, $"Удалена категория с ID {id}", "delete-category", ct);
                return NoContent();
            }

            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }

            catch (Exception ex)
            {
                await _brokerService.SendMessageToLogAsync(LogLevel.Critical, ex.Message, "delete-category", ct);
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера" });
            }
        }

        // GET: api/categories/5
        [HttpGet("{id:int}", Name = "GetCategoryRoomAsync")]
        public async Task<ActionResult<CategoryRoom>> GetCategoryRoomAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var room = await _categoryRoomsService.GetCategoryRoomByIdAsync(id, ct);
                if (room == null)
                {
                    var message = $"Категория с ID {id} не найдена";
                    await _brokerService.SendMessageToLogAsync(LogLevel.Warning, message, "get-category", ct);
                    return NotFound(new { Message = message });
                }
                await _brokerService.SendMessageToLogAsync(LogLevel.Information, $"Получена категория с ID {id}", "get-category", ct);
                return Ok(room);
            }

            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }

            catch (Exception ex)
            {
                await _brokerService.SendMessageToLogAsync(LogLevel.Critical, ex.Message, "get-category", ct);
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера" });
            }
        }
    }
}
