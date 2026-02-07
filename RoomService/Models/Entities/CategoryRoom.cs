using System.Formats.Asn1;

namespace RoomService.Models.Entities
{
    public class CategoryRoomDto
    {
        public int Id { get; }
        public string? Name { get; private set; }
        public  string? Description { get; private set; }
    }
}
