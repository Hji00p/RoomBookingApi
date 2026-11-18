using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MeetingRoomBooking.Domain.Entities;

namespace MeetingRoomBooking.Application.Abstractions;

public interface IBookingRepository
{
    Task<IReadOnlyList<Booking>> ListActiveForDateAsync(
        int roomId,
        DateOnly date,
        CancellationToken cancellationToken);

    Task<bool> TryCreateAsync(
    Booking booking,
    CancellationToken cancellationToken);

    Task<bool> CancelAsync(int bookingId, CancellationToken cancellationToken);
}