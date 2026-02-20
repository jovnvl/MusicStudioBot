namespace BookingService.Models.Entities
{
    public class Booking
    {
        public int Id { get; }
        public User User { get; }
        public Room Room { get; }       
        public DateTime CreationDate { get; }
        public int? Status { get; }
        public Booking(int id, User user, Room room, DateTime creationDate, int? status)
        {
            Id = id;
            User  = user;
            Room = room;
            CreationDate = creationDate;
            Status = status;
        }
    }
}
