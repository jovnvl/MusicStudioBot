using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using BookingService.DTO;
using BookingService.Models.Entities;
using BookingService.Services;

namespace BookingService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomController : ControllerBase
    {
        private readonly IRoomService _roomsService;
        public RoomController(IRoomService roomService)
        {
            _roomsService = roomService;
        }

        // GET: api/rooms
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<Room>>> GetRoomsAsync(CancellationToken ct = default)
        {
            try
            {
                var rooms = await _roomsService.GetAllRoomsAsync(ct);
                return Ok(rooms);
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

        // GET: api/rooms/5
        [HttpGet("{id:int}", Name = "GetRoomAsync")]
        public async Task<ActionResult<Room>> GetRoomAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var room = await _roomsService.GetRoomByIdAsync(id, ct);
                if (room == null)
                {
                    return NotFound(new { Message = $"Кабиент с ID {id} не найден" });
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

        // POST: api/rooms
        [HttpPost]
        public async Task<ActionResult<Room>> CreateRoomAsync(RoomDto roomDto, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            try
            {
                var createdRoom = await _roomsService.CreateRoomAsync(roomDto, ct);

                if (createdRoom == null)
                    return BadRequest(new { Message = "Не удалось создать кабинет" });

                return CreatedAtRoute(
                    "GetRoomAsync",
                    new { id = createdRoom.Id },
                    createdRoom);
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

        // DELETE: api/rooms/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteRoomAsync(int id, CancellationToken ct = default)
        {
            try
            {
                //var exists = await _roomsService.ExistsRoomAsync(id, ct);
                //if (!exists)
                //{
                //    return NotFound(new { Message = $"Комната с ID {id} не найдена" });
                //}

                var deleted = await _roomsService.DeleteRoomAsync(id, ct);
                if (!deleted)
                {
                    return BadRequest(new { Message = "Не удалось удалить кaбанет" });
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
    }
}
