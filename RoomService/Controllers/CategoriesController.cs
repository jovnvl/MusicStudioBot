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
        public CategoriesController(ICategoryRoomService categoryRoomService)
        {
            _categoryRoomsService = categoryRoomService;
        }

        // GET: api/categories
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<CategoryRoom>>> GetAllCategoriesAsync(CancellationToken ct = default)
        {
            var categoryRooms = await _categoryRoomsService.GetAllCategoryRoomAsync(ct);
            return Ok(categoryRooms);
        }

        // POST: api/categories
        [HttpPost]
        public async Task<ActionResult<CategoryRoom>> CreateCategoryRoomAsync(CreateCategoryRoomDto categoryRoomDto, CancellationToken ct = default)
        {
            var createdCategory = await _categoryRoomsService.CreateCategoryRoomAsync(categoryRoomDto, ct);

            if (createdCategory == null)
            {
                return BadRequest(new { Message = "Не удалось создать категорию" });
            }

            return CreatedAtRoute("GetCategoryRoomAsync",new { id = createdCategory.Id }, createdCategory);
          
        }

        // DELETE: api/categories/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteRoomAsync(int id, CancellationToken ct = default)
        {
            var deleted = await _categoryRoomsService.DeleteCategoryRoomAsync(id, ct);
            if (!deleted)
            {
                return BadRequest(new { Message = "Не удалось удалить категорию" });
            }
            return NoContent();
        }

        // GET: api/categories/5
        [HttpGet("{id:int}", Name = "GetCategoryRoomAsync")]
        public async Task<ActionResult<CategoryRoom>> GetCategoryRoomAsync(int id, CancellationToken ct = default)
        {

            var room = await _categoryRoomsService.GetCategoryRoomByIdAsync(id, ct);
            if (room == null)
            {
                return NotFound(new { Message = $"Категория с ID {id} не найдена" });
            }
            return Ok(room);

        }
    }
}
