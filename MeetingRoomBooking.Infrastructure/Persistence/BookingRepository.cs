using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MeetingRoomBooking.Application.Abstractions;
using MeetingRoomBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MeetingRoomBooking.Infrastructure.Persistence;

public sealed class BookingRepository(AppDbContext db) : IBookingRepository
{
    public async Task<IReadOnlyList<Booking>> ListActiveForDateAsync(
        int roomId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var dayStartUtc = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dayEndUtc = dayStartUtc.AddDays(1);

        return await db.Bookings
            .AsNoTracking()
            .Where(booking =>
                booking.RoomId == roomId &&
                booking.CancelledAtUtc == null &&
                booking.StartUtc < dayEndUtc &&
                booking.EndUtc > dayStartUtc)
            .OrderBy(booking => booking.StartUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> TryCreateAsync(
    Booking booking,
    CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.ReadCommitted,
            cancellationToken);

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""SELECT "Id" FROM "Rooms" WHERE "Id" = {booking.RoomId} FOR UPDATE""",
            cancellationToken);

        var hasOverlap = await db.Bookings.AnyAsync(
            existing =>
                existing.RoomId == booking.RoomId &&
                existing.CancelledAtUtc == null &&
                existing.StartUtc < booking.EndUtc &&
                existing.EndUtc > booking.StartUtc,
            cancellationToken);

        if (hasOverlap)
        {
            return false;
        }

        db.Bookings.Add(booking);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return true;
    }

    public async Task<bool> CancelAsync(
    int bookingId,
    CancellationToken cancellationToken)
    {
        var cancelledAtUtc = DateTime.UtcNow;

        var updatedRows = await db.Bookings
            .Where(booking => booking.Id == bookingId && booking.CancelledAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    booking => booking.CancelledAtUtc,
                    (DateTime?)cancelledAtUtc),
                cancellationToken);

        return updatedRows > 0 || await db.Bookings.AnyAsync(
            booking => booking.Id == bookingId,
            cancellationToken);
    }
}