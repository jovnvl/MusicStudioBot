namespace BookingService.Models.Entities
{
    public class Booking
    {
        public Guid Id { get; set; }
        public User User { get; set; }
        public Room Room { get; set;  }
        public DateTime CreationDate { get; set; } = DateTime.UtcNow;
        public BookingStatus? Status { get; set; } = BookingStatus.NotConfirmed;
        public DateTime? TimeBegin { get; set; }
        public DateTime? TimeEnd { get; set; }
        public string? Description { get; set; }       
    }
}
