using System.Formats.Asn1;

namespace RoomService.Models.Entities
{
    public class CategoryRoom
    {
        int Id { get; }
        string? Name { get; set; }
        string? Description { get; set; }
    }
}
