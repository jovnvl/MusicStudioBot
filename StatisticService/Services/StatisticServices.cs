using StackExchange.Redis;

namespace StatisticService.Services
{
    public class StatisticServices : IStatisticService
    {
        private readonly IDatabase _redisDb;
        private readonly string _key = "stats:total_bookings";

        public StatisticServices(IConnectionMultiplexer redis)
        {
            _redisDb = redis.GetDatabase();
        }

        public async Task IncrementBookingCountAsync(CancellationToken ct)
        {
            // Увеличиваем счетчик на 1
            await _redisDb.StringIncrementAsync(_key);
        }

        public async Task<long> GetBookingCountAsync(CancellationToken ct)
        {
            // Получаем текущее значение
            var value = await _redisDb.StringGetAsync(_key);
            return value.HasValue ? (long)value : 0;
        }
    }
}
