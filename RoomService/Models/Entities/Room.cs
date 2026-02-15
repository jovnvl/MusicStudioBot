namespace RoomService.Models.Entities
{
    public class Room
    {
        public int Id { get; }
        public string? Name { get; }
        public string? Description { get; }
        public byte[]? Photo { get; }
        public DateOnly CreationDate { get; }
        public CategoryRoom CategoryRoomId { get; }
        public int? Status { get; }
        public Room(int id, string? name, string? description, byte[]? photo, DateOnly creationDate, CategoryRoom categoryRoomId, int? status)
        {
            Id = id;
            Name = name;
            Description = description;
            Photo = photo;
            CreationDate = creationDate;
            CategoryRoomId = categoryRoomId;
            Status = status;
        }
    }
}
