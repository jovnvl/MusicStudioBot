namespace RoomService.Models.Entities
{
    public class Room
    {
        public int Id { get; private set; }
        public string? Name { get; private set; }
        public string? Description { get; private set; }
        public byte[]? Photo { get; private set; }
        public DateOnly CreationDate { get; private set; }
        public CategoryRoomDto CategoryRoomId { get; private set; }
        public int? Status { get; private set; }
        public Room(int id, string? name, string? description, byte[]? photo, DateOnly creationDate, CategoryRoomDto categoryRoomId, int? status)
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
