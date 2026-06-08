using BookingService.Infrastructure.Concurrency;
namespace BookingService.Infrastructure.Concurrency
{
    using System.Collections.Concurrent;

    public sealed class BookingLockProvider : IBookingLockProvider
    {
        private readonly ConcurrentDictionary<int, SemaphoreSlim> _locks = new();

        public SemaphoreSlim GetLock(int roomId)
        {
            return _locks.GetOrAdd(
                roomId,
                _ => new SemaphoreSlim(1, 1));
        }
    }
}
