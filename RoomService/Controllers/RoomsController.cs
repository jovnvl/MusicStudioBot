using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using RoomService.DTO;
using RoomService.Infrastructure;
using RoomService.Models.Entities;
using RoomService.Repositories;
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

        public RoomsController(IRoomsService roomService, IMessageBrokerService brokerService, IOutboundMessagesService outboundMessagesService)
        {
            _roomsService = roomService;
        }

        // GET: api/rooms
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<Room>>> GetRoomsAsync(CancellationToken ct = default)
        {
            var rooms = await _roomsService.GetAllRoomsAsync(ct);
            return Ok(rooms);
        }

        // GET: api/rooms/5
        [HttpGet("{id:int}", Name = "GetRoomAsync")]
        public async Task<ActionResult<Room>> GetRoomAsync(int id, CancellationToken ct = default)
        {
            var room = await _roomsService.GetRoomByIdAsync(id, ct);
            if (room == null)
            {
                return NotFound(new { Message = $"Комната с ID {id} не найдена" });
            }
            return Ok(room);
        }

        // POST: api/rooms
        [HttpPost]
        public async Task<ActionResult<Room>> CreateRoomAsync(CreateRoomDto roomDto, CancellationToken ct = default)
        {
            var createdRoom = await _roomsService.CreateRoomAsync(roomDto, ct);
                            
            if (createdRoom == null)
            {
                return BadRequest(new { Message = "Не удалось создать комнату" });
            }
            return CreatedAtRoute("GetRoomAsync", new { id = createdRoom.Id }, createdRoom);
        }

        // DELETE: api/rooms/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteRoomAsync(int id, CancellationToken ct = default)
        {
            var deleted = await _roomsService.DeleteRoomAsync(id, ct);
            if (!deleted)
            {
                return BadRequest(new { Message = $"Не удалось удалить комнату с ID {id}" });
            }
            return Ok();
        }

        // PUT: api/rooms
        [HttpPut]
        public async Task<ActionResult<Room>> UpdateRoomAsync(UpdateRoomDto updateRoomDto, CancellationToken ct = default)
        {
            var createdRoom = await _roomsService.UpdateRoomAsync(updateRoomDto, ct);

            if (!createdRoom)
            {
                return BadRequest(new { Message = "Не удалось обновить комнату" });
            }
            return Ok();
        }
    }
}
