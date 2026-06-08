namespace BookingService.Infrastructure.Concurrency
{
    public interface IBookingLockProvider
    {
        SemaphoreSlim GetLock(int roomId);
    }
}
