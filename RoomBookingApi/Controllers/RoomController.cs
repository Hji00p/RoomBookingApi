using MeetingRoomBooking.Api.Contracts;
using MeetingRoomBooking.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace MeetingRoomBooking.Api.Controllers;

[ApiController]
[Route("api/rooms")]
public sealed class RoomsController(
    IRoomRepository rooms,
    IBookingRepository bookings) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<RoomResponse[]>> GetRooms(
        CancellationToken cancellationToken)
    {
        var result = await rooms.ListAsync(cancellationToken);
        return Ok(result.Select(room => new RoomResponse(room.Id, room.Name)).ToArray());
    }

    [HttpGet("{roomId:int}/bookings")]
    public async Task<ActionResult<BookingResponse[]>> GetRoomBookings(
    int roomId,
    [FromQuery] DateOnly? date,
    CancellationToken cancellationToken)
    {
        if (date is not { } selectedDate || selectedDate == DateOnly.MaxValue)
        {
            return Problem(
                statusCode: 400,
                detail: "Provide a valid date in YYYY-MM-DD format with a following day.");
        }

        if (!await rooms.ExistsAsync(roomId, cancellationToken))
        {
            return Problem(statusCode: 404, detail: "Room not found.");
        }

        var schedule = await bookings.ListActiveForDateAsync(
            roomId, selectedDate, cancellationToken);

        var response = schedule
            .Select(booking => new BookingResponse(
                booking.Id,
                booking.RoomId,
                booking.Title,
                booking.StartUtc,
                booking.EndUtc))
            .ToArray();

        return Ok(response);
    }
}