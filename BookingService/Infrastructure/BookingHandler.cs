using BookingService.Infrastructure.Events;
using BookingService.Models.Entities;
using BookingService.Repositories;
using MediatR;
using Microsoft.AspNetCore.Components.Web;
using System.Reflection.Metadata;

namespace BookingService.Infrastructure
{
    public interface IEventHandler<in TEvent>
    {
        Task Handle(TEvent evt, CancellationToken ct);
    }
    //MediatR
    //здесь определение события Event через record
    //public record BookingCreatedEvent(Guid Id) : INotification;
    public class BookingCreatedHandler : INotificationHandler<BookingCreatedEvent>
    {
        private readonly ILogger<BookingCreatedHandler> _logger;
        public BookingCreatedHandler(ILogger<BookingCreatedHandler> logger)
        {
            _logger = logger;
        }
        public Task Handle(BookingCreatedEvent evt, CancellationToken ct)
        {
            //произошло событие создания брони
            _logger.LogInformation($"Booking created: Id {evt.Id}");
            //Console.WriteLine($"Booking created: {evt.Id}");
            return Task.CompletedTask;
        }
    }

    //public record BookingDeletedEvent(Guid Id) : INotification;
    /*
    public class BookingDeletedHandler : IEventHandler<BookingDeletedEvent>
    {
        private readonly IRabbitMQPublisher? _publisher;
        public async Task Handle(BookingDeletedEvent evt, CancellationToken ct)
        {
            Console.WriteLine($"Booking deleted: {evt.Id}");

            if (_publisher != null && evt != null )
                await _publisher.PublishAsync(
                Common.Constants.LOGIN_SERVICE_QUEUE,
                evt, ct);
        } 
    }
    */
    
}
