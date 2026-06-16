using BookingService.Infrastructure.Concurrency;
using Microsoft.Extensions.Caching.Memory;

namespace BookingService.Infrastructure.Concurrency
{
    
    public sealed class BookingLockProvider : IBookingLockProvider
    {
        private readonly IMemoryCache _cache;

        public BookingLockProvider(IMemoryCache cache)
        {
            _cache = cache;
        }

        public SemaphoreSlim GetLock(int roomId)
        {
            return _cache.GetOrCreate(roomId, entry =>
            {
                entry.SlidingExpiration =
                    TimeSpan.FromMinutes(10);

                return new SemaphoreSlim(1, 1);
            })!;
        }
    }
}
