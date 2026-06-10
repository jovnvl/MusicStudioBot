using BookingService.DTO;
using BookingService.Models.Entities;

namespace BookingService.Models.Mapping
{
    public static class BookingMapping
    {
        public static BookingDto ToDto(this Booking b) => new BookingDto()
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

        public static Booking ToEntity(this BookingDto dto) => Booking.Clone(
            id: dto.Id,
            creationDate: dto.CreationDate,
            userId: dto.UserId,
            roomId: dto.RoomId,
            period: BookingPeriod.Create(dto.TimeBegin, dto.TimeEnd),
            status: dto.Status,
            description: dto.Description
            );
    }
}