using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using RoomService.DTO;
using RoomService.Infrastructure;
using RoomService.Models.Entities;
using RoomService.Services;
using System.Text;
using System.Text.Json;

namespace RoomService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomsController : ControllerBase
    {
        private readonly IRoomsService _roomsService;
        private readonly IMessageBrokerService _brokerService;
        public RoomsController(IRoomsService roomService, IMessageBrokerService brokerService)
        {
            _roomsService = roomService;
            _brokerService = brokerService;
        }

        // GET: api/rooms
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<Room>>> GetRoomsAsync(CancellationToken ct = default)
        {
            try
            {
                var rooms = await _roomsService.GetAllRoomsAsync(ct);
                await _brokerService.SendMessageToLogAsync(LogLevel.Information, "Успешно запрошен список всех комнат", "get-rooms", ct);
                return Ok(rooms);
            }
            
            catch (OperationCanceledException)
            {
                return StatusCode(499); 
            }
            
            catch (Exception ex)
            {
                await _brokerService.SendMessageToLogAsync(LogLevel.Critical, ex.Message, "get-rooms", ct);
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера" });
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
                    var message = $"Комната с ID {id} не найдена";
                    await _brokerService.SendMessageToLogAsync(LogLevel.Warning, message, "get-room", ct);
                    return NotFound(new { Message = message });
                }
                await _brokerService.SendMessageToLogAsync(LogLevel.Information, $"Комната с ID {id} найдена", "get-room", ct);
                return Ok(room);
            }
            
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            
            catch (Exception ex)
            {
                await _brokerService.SendMessageToLogAsync(LogLevel.Critical, ex.Message, "get-room", ct);
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера" });
            }
        }

        // POST: api/rooms
        [HttpPost]
        public async Task<ActionResult<Room>> CreateRoomAsync(CreateRoomDto roomDto, CancellationToken ct = default)
        {
            try
            {
                var createdRoom = await _roomsService.CreateRoomAsync(roomDto, ct);
                            
                if (createdRoom == null)
                {
                    var message = "Не удалось создать комнату";
                    await _brokerService.SendMessageToLogAsync(LogLevel.Warning, message, "post-rooms", ct);
                    return BadRequest(new { Message = message });
                }

                await _brokerService.SendMessageToLogAsync(LogLevel.Information, $"Создана комната с ID {createdRoom.Id}", "post-rooms", ct);
                return CreatedAtRoute("GetRoomAsync", new { id = createdRoom.Id }, createdRoom);
            }
            
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            
            catch (Exception ex)
            {
                await _brokerService.SendMessageToLogAsync(LogLevel.Critical, ex.Message, "post-room", ct);
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера" });
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
                    var message = $"Не удалось удалить комнату с ID {id}";
                    await _brokerService.SendMessageToLogAsync(LogLevel.Warning, message, "delete-room", ct);
                    return BadRequest(new { Message = message });
                }
                await _brokerService.SendMessageToLogAsync(LogLevel.Information, $"Комната с ID {id} удалена", "delete-room", ct);
                return Ok();
            }

            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }

            catch (Exception ex)
            {
                await _brokerService.SendMessageToLogAsync(LogLevel.Critical, ex.Message, "delete-room", ct);
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера" });
            }
        }

        // PUT: api/rooms
        [HttpPut]
        public async Task<ActionResult<Room>> UpdateRoomAsync(UpdateRoomDto updateRoomDto, CancellationToken ct = default)
        {
            try
            {
                var createdRoom = await _roomsService.UpdateRoomAsync(updateRoomDto, ct);

                if (!createdRoom)
                {
                    var message = "Не удалось обновить комнату";
                    await _brokerService.SendMessageToLogAsync(LogLevel.Warning, message, "update-room", ct);
                    return BadRequest(new { Message =  message});
                }

                await _brokerService.SendMessageToLogAsync(LogLevel.Information, $"Комната с ID {updateRoomDto.Id} обновлена", "update-room", ct);
                return Ok();
            }

            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }

            catch (Exception ex)
            {
                await _brokerService.SendMessageToLogAsync(LogLevel.Critical, ex.Message, "update-room", ct);
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера" });
            }
        }
    }
}
