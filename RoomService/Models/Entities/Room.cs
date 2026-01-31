namespace RoomService.Models.Entities
{
    public class Room
    {
        int Id { get; }
        string? Name { get; set; }
        string? Description { get; set; }
        byte[]? Photo { get; set; }
        DateOnly CreationDate { get; set; }
        CategoryRoom CategoryRoomId { get; set; }
        int? Status { get; set; }
    }
}
