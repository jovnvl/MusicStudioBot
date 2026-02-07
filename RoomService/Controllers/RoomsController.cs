using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RoomService.Models.Entities;
using RoomService.Services;

namespace RoomService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomsController : ControllerBase
    {
        private IRoomsService _roomService;
        public RoomsController(IRoomsService roomService)
        {
            _roomService = roomService;
        }

        // GET: api/rooms
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Room>>> GetRooms(CancellationToken ct = default)
        {
            try
            {
                var rooms = await _roomService.GetAllRoomsAsync(ct);
                return Ok(rooms);
            }
            catch (Exception ex)
            {
                // Логирование ошибки
                //_logger.LogError(ex, "Ошибка при получении списка комнат");

                // Возвращаем 500
                return StatusCode(500, new
                {
                    Message = "Внутренняя ошибка сервера",
                    Error = ex.Message
                });
            }
        }

        // GET: api/rooms/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Room>> GetRoom(int id, CancellationToken ct = default)
        {
            try
            {
                var room = await _roomService.GetRoomAsync(id, ct);
                if (room == null)
                {
                    return NotFound(new { Message = $"Комната с ID {id} не найдена" });
                }
                return Ok(room);
            }
            catch (Exception ex)
            {
                // Логирование ошибки
                //_logger.LogError(ex, "Ошибка при получении комнаты");

                // Возвращаем 500
                return StatusCode(500, new
                {
                    Message = "Внутренняя ошибка сервера",
                    Error = ex.Message
                });

            }
        }

        // POST: api/rooms
        //[HttpPost]
        //public async Task<ActionResult<Room>> CreateRoom(CreateRoomDto roomDto)
        //{
        //    var createdRoom = await _roomService.CreateRoomAsync(roomDto);

        //    return CreatedAtAction(
        //        nameof(GetRoom),
        //        new { id = createdRoom.Id },
        //        createdRoom);
        //}

        // DELETE: api/rooms/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteRoom(int id, CancellationToken ct = default)
        {
            var exists = await _roomService.ExistsRoomAsync(id, ct);
            if (!exists)
            {
                return NotFound(new { Message = $"Комната с ID {id} не найдена" });
            }

            var deleted = await _roomService.DeleteRoomAsync(id, ct);
            if (!deleted)
            {
                return BadRequest(new { Message = "Не удалось удалить комнату" });
            }

            return NoContent();
        }
    }
}
