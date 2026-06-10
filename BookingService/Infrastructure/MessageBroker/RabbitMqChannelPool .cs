using System.Collections.Concurrent;
using RabbitMQ.Client;

namespace BookingService.Infrastructure.MessageBroker
{
    public sealed class RabbitMqChannelPool : IAsyncDisposable
    {
        private readonly ConnectionFactory _factory;

        private readonly ConcurrentQueue<IChannel> _channels = new();

        private readonly SemaphoreSlim _poolSemaphore;
        private readonly SemaphoreSlim _connectionLock = new(1, 1);

        private IConnection? _connection;

        public RabbitMqChannelPool(
            ConnectionFactory factory,
            int maxChannels = 20)
        {
            _factory = factory;
            _poolSemaphore = new SemaphoreSlim(maxChannels, maxChannels);
        }

        private async Task<IConnection> GetConnectionAsync(
            CancellationToken ct)
        {
            if (_connection?.IsOpen == true)
                return _connection;

            await _connectionLock.WaitAsync(ct);

            try
            {
                if (_connection?.IsOpen == true)
                    return _connection;

                _connection = await _factory.CreateConnectionAsync(ct);

                return _connection;
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        public async Task<IChannel> RentAsync(
            CancellationToken ct)
        {
            await _poolSemaphore.WaitAsync(ct);

            try
            {
                while (_channels.TryDequeue(out var channel))
                {
                    if (channel.IsOpen)
                        return channel;

                    await channel.DisposeAsync();
                }

                var connection = await GetConnectionAsync(ct);

                return await connection.CreateChannelAsync(
                    cancellationToken: ct);
            }
            catch
            {
                _poolSemaphore.Release();
                throw;
            }
        }

        public void Return(IChannel channel)
        {
            if (channel.IsOpen)
            {
                _channels.Enqueue(channel);
            }

            _poolSemaphore.Release();
        }

        public async ValueTask DisposeAsync()
        {
            while (_channels.TryDequeue(out var channel))
            {
                await channel.DisposeAsync();
            }

            if (_connection is not null)
            {
                await _connection.DisposeAsync();
            }

            _poolSemaphore.Dispose();
            _connectionLock.Dispose();
        }
    }
}
