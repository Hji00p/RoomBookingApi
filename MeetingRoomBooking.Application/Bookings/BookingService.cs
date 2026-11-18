using System;
using System.Threading;
using System.Threading.Tasks;
using MeetingRoomBooking.Application.Abstractions;
using MeetingRoomBooking.Domain.Entities;

namespace MeetingRoomBooking.Application.Bookings;

public enum CreateBookingStatus
{
    Created,
    InvalidInput,
    RoomNotFound,
    Conflict
}

public sealed class BookingService(
    IRoomRepository rooms,
    IBookingRepository bookings)
{
    public async Task<(CreateBookingStatus Status, Booking? Booking)> CreateAsync(
        int roomId,
        string? title,
        DateTimeOffset? start,
        DateTimeOffset? end,
        CancellationToken cancellationToken)
    {
        if (roomId <= 0 ||
            string.IsNullOrWhiteSpace(title) ||
            start is null ||
            end is null ||
            end.Value <= start.Value)
        {
            return (CreateBookingStatus.InvalidInput, null);
        }

        if (!await rooms.ExistsAsync(roomId, cancellationToken))
        {
            return (CreateBookingStatus.RoomNotFound, null);
        }

        var booking = new Booking(
            roomId, title, start.Value.UtcDateTime, end.Value.UtcDateTime);

        if (!await bookings.TryCreateAsync(booking, cancellationToken))
        {
            return (CreateBookingStatus.Conflict, null);
        }

        return (CreateBookingStatus.Created, booking);
    }

    public Task<bool> CancelAsync(
        int bookingId,
        CancellationToken cancellationToken)
    {
        return bookings.CancelAsync(bookingId, cancellationToken);
    }
}