namespace BookingService.Models.Entities
{
    public class Booking
    {
        public Guid Id { get; set; }
        public User User { get; }
        public Room Room { get; }
        public DateTime CreationDate { get; set; } = DateTime.UtcNow;
        public BookingStatus? Status { get; set; } = BookingStatus.NotConfirmed;
        public DateTime? TimeBegin { get; }
        public DateTime? TimeEnd { get; }
        public string? Description { get; }
        public Booking(User user, Room room, DateTime creationDate, BookingStatus? status, DateTime? timeBegin, DateTime? timeEnd, string? description)
        {
            User  = user;
            Room = room;
            CreationDate = creationDate;
            Status = status;
            TimeBegin = timeBegin;
            TimeEnd = timeEnd;
            Description = description;
        }
    }
}
