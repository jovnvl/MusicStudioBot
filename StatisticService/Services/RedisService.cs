using StackExchange.Redis;

namespace StatisticService.Services
{
    public class RedisService : IStatisticService
    {
        private readonly IDatabase _redisDb;

        public RedisService(IConnectionMultiplexer redis)
        {
            _redisDb = redis.GetDatabase();
        }

        public async Task IncrementBookingCountAsync(CancellationToken ct)
        {
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            await _redisDb.StringIncrementAsync($"stats:bookings:{today}");
            await _redisDb.HashIncrementAsync("stats:bookings:daily", today, 1);
        }

        public async Task IncrementDeleteBookingCountAsync(CancellationToken ct)
        {
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            await _redisDb.StringIncrementAsync($"stats:deletebookings:{today}");
            await _redisDb.HashIncrementAsync("stats:deletebookings:daily", today, 1);
        }

        public async Task IncrementUpdateBookingCountAsync(CancellationToken ct)
        {
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            await _redisDb.StringIncrementAsync($"stats:updatebookings:{today}");
            await _redisDb.HashIncrementAsync("stats:updatebookings:daily", today, 1);
        }

        public async Task<long> GetBookingCountAsync(CancellationToken ct)
        {
            var value = await _redisDb.StringGetAsync(GetCurrentKey());
            return value.HasValue ? (long)value : 0;
        }

        private string GetCurrentKey()
        {
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            return $"stats:bookings:{today}";
        }
    }
}
