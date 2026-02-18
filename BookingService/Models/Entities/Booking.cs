namespace BookingService.Models.Entities
{
    public class Booking
    {
        public int Id { get; }
        public int UserId { get; }
        public int RoomId { get; }       
        public DateTime CreationDate { get; }
        public int? Status { get; }
        public Booking(int id, int userId, int roomId, DateTime creationDate, int? status)
        {
            Id = id;
            UserId  = userId;
            RoomId = roomId;
            CreationDate = creationDate;
            Status = status;
        }
    }
}
