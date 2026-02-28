using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using BookingService.DTO;
using BookingService.Models.Entities;
using BookingService.Services;

namespace BookingService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryRoomController : ControllerBase
    {
        private readonly ICategoryRoomService _categoryRoomsService;
        public CategoryRoomController(ICategoryRoomService categoryRoomService)
        {
            _categoryRoomsService = categoryRoomService;
        }

        // GET: api/categories
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<CategoryRoom>>> GetAllCategoriesAsync(CancellationToken ct = default)
        {
            try
            {
                var categoryRooms = await _categoryRoomsService.GetAllCategoryRoomAsync(ct);
                return Ok(categoryRooms);
            }
            
            catch (OperationCanceledException)
            {
                return StatusCode(499); 
            }
            
            catch (Exception ex)
            {
                // Добавить логирование ошибки

                return StatusCode(500, new
                {
                    Message = "Внутренняя ошибка сервера",
                    //Error = ex.Message  GUID ошибки в логгере
                });
            }
        }

        // POST: api/categories
        [HttpPost]
        public async Task<ActionResult<CategoryRoom>> CreateCategoryRoomAsync(CategoryRoomDto categoryRoomDto, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            try
            {
                var createdCategory = await _categoryRoomsService.CreateCategoryRoomAsync(categoryRoomDto, ct);

                if (createdCategory == null)
                    return BadRequest(new { Message = "Не удалось создать категорию" });

                return CreatedAtRoute(
                    "GetCategoryRoomAsync",
                    new { id = createdCategory.Id },
                    createdCategory);
            }
            
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            
            catch (Exception ex)
            {
                // Добавить логирование ошибки

                return StatusCode(500, new
                {
                    Message = "Внутренняя ошибка сервера",
                    //Error = ex.Message    GUID ошибки в логгере
                });
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
                    return BadRequest(new { Message = "Не удалось удалить категорию" });
                }

                return Ok();
            }

            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }

            catch (Exception ex)
            {
                // Добавить логирование ошибки

                return StatusCode(500, new
                {
                    Message = "Внутренняя ошибка сервера",
                    //Error = ex.Message    GUID ошибки в логгере
                });
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
                    return NotFound(new { Message = $"Категория с ID {id} не найдена" });
                }
                return Ok(room);
            }

            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }

            catch (Exception ex)
            {
                // Добавить логирование ошибки

                return StatusCode(500, new
                {
                    Message = "Внутренняя ошибка сервера",
                    //Error = ex.Message    GUID ошибки в логгере
                });

            }
        }
    }
}
