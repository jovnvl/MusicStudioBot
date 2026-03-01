using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RoomService.DTO;
using RoomService.Models.Entities;
using RoomService.Services;

namespace RoomService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomsController : ControllerBase
    {
        private readonly IRoomsService _roomsService;
        public RoomsController(IRoomsService roomService)
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
                    return NotFound(new { Message = $"Комната с ID {id} не найдена" });
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
        public async Task<ActionResult<Room>> CreateRoomAsync(CreateRoomDto roomDto, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            try
            {
                var createdRoom = await _roomsService.CreateRoomAsync(roomDto, ct);

                if (createdRoom == null)
                    return BadRequest(new { Message = "Не удалось создать комнату" });

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
                    return BadRequest(new { Message = "Не удалось удалить комнату" });
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

        // PUT: api/rooms
        [HttpPut]
        public async Task<ActionResult<Room>> UpdateRoomAsync(UpdateRoomDto updateRoomDto, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            try
            {
                var createdRoom = await _roomsService.UpdateRoomAsync(updateRoomDto, ct);

                if (!createdRoom)
                    return BadRequest(new { Message = "Не удалось обновить комнату" });

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
