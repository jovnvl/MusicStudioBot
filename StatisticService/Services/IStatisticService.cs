namespace StatisticService.Services
{
    public interface IStatisticService
    {
        Task IncrementBookingCountAsync(CancellationToken ct);
        Task IncrementDeleteBookingCountAsync(CancellationToken ct);
        Task<long> GetBookingCountAsync(CancellationToken ct);
    }
}
