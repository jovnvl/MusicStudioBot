using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingService.Models.Entities
{
    public class Booking
    {
        public Guid Id { get; private set; }
        [Required]
        [Column("UserId")]
        public Guid UserId { get; private set; }

        [Required]
        [Column("RoomId")]
        public int RoomId { get; private set; }

        public DateTime CreationDate { get; private set; } = DateTime.UtcNow;
        public BookingStatus? Status { get; private set; } = BookingStatus.NotConfirmed;
        public BookingPeriod Period { get; private set; } = new(DateTime.UtcNow, DateTime.UtcNow);
        public string? Description { get; private set; } = string.Empty;
        public static Booking Create(
                Guid userId,
                int roomId,
                BookingStatus? status,
                BookingPeriod period,
                string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                throw new InvalidOperationException(
                    "Description is required");

            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                CreationDate = DateTime.UtcNow,
                UserId= userId,
                RoomId = roomId,
                Status = status,
                Period = period,
                Description = description
            };

            return booking;
        }
        public void SetInstance(
            DateTime creationDate,
            Guid userId,
            int roomId,
            BookingStatus? status,
            BookingPeriod period,
            string description)
        {
            CreationDate = creationDate;
            UserId = userId;
            RoomId = roomId;
            Status = status;
            Period = period;
            Description = description;
        }

        public static Booking Clone(
        Guid id,
        DateTime creationDate,
        Guid userId,
        int roomId,
        BookingStatus? status,
        BookingPeriod period,
        string description)
        {
            return new Booking
            {
                Id = id,
                CreationDate = creationDate,
                UserId = userId,
                RoomId = roomId,
                Status = status,
                Period = period,
                Description = description
            };
        }

        public void NotConfirmed() { Status = BookingStatus.NotConfirmed; }
        public void Canceled() { Status = BookingStatus.Canceled; }
        public void Booked() { Status = BookingStatus.Booked; }
        public void Completed() { Status = BookingStatus.Completed; }
        public void Reschedule(BookingPeriod newPeriod)
        {
            Period = BookingPeriod.Create(newPeriod.TimeBegin, newPeriod.TimeEnd);
        }
        public void RescheduleToRoom(BookingPeriod newPeriod, int roomId)
        {
            Period = BookingPeriod.Create(newPeriod.TimeBegin, newPeriod.TimeEnd);
            RoomId = roomId;
        }
    }
}
