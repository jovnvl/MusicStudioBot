using GatewayService.Models.Enums;

namespace GatewayService.Models.DTOs
{
    public class UpdateRoomRequest
    {
        public int Id { get; set; }
        public RoomStatus Status { get; set; }
    }
}
