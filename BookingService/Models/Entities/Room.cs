namespace BookingService.Models.Entities
{
    public class Room
    {
        public int Id { get; }
        public string? Name { get; }
        public string? Description { get; }
        public byte[]? Photo { get; }
        public DateOnly CreationDate { get; }
        public CategoryRoom CategoryRoom { get; }
        public int? Status { get; }
        public Room(int id, string? name, string? description, byte[]? photo, DateOnly creationDate, CategoryRoom categoryRoom, int? status)
        {
            Id = id;
            Name = name;
            Description = description;
            Photo = photo;
            CreationDate = creationDate;
            CategoryRoom = categoryRoom;
            Status = status;
        }
    }
}
