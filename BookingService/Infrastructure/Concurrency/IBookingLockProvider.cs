using BookingService.Models.Entities;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BookingService.Infrastructure.Concurrency
{
    public interface IBookingLockProvider
    {
        SemaphoreSlim GetLock(int roomId);
/*
лучше — transaction-level advisory lock
PostgreSQL имеет специальную функцию:
pg_advisory_xact_lock(bigint)
Она автоматически освобождается при завершении транзакции.

Пример:

await using var transaction =
    await _context.Database.BeginTransactionAsync(ct);

await _context.Database.ExecuteSqlInterpolatedAsync(
    $"SELECT pg_advisory_xact_lock({booking.RoomId})",
        ct);

await _validationPipeline.ValidateAsync(
        booking,
        ct);

await _repository.AddAsync(
        booking,
        ct);

await transaction.CommitAsync(ct);
*/
    }
}
