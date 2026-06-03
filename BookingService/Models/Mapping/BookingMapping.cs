using BookingService.DTO;
using BookingService.Models.Entities;

namespace BookingService.Models.Mapping
{
    public static class BookingMapping
    {
        public static BookingDto ToDto(this Booking b) => new()
        {
            Id = b.Id,
            CreationDate = b.CreationDate,
            UserId = b.UserId,
            RoomId = b.RoomId,
            TimeBegin = b.Period.TimeBegin,
            TimeEnd = b.Period.TimeEnd,
            Status = (b.Status) ?? BookingStatus.NotConfirmed,
            Description = (b.Description) ?? string.Empty
        };

        public static Booking ToEntity(this BookingDto dto) => new()
        {
            Id = dto.Id,
            CreationDate = dto.CreationDate,
            UserId = dto.UserId,
            RoomId = dto.RoomId,
            Period = new BookingPeriod(dto.TimeBegin, dto.TimeEnd),
            Status = dto.Status,
            Description = dto.Description
        };
    }
}
