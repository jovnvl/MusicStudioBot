using GatewayService.Models.Enums;

namespace GatewayService.DTO

{
    public class CreateRoomRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public byte[]? Photo { get; set; }
        public int CategoryRoomId { get; set; }
        public RoomStatus Status { get; set; }
    }
}
