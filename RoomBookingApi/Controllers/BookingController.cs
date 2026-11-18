using MeetingRoomBooking.Api.Contracts;
using MeetingRoomBooking.Application.Bookings;
using Microsoft.AspNetCore.Mvc;

namespace MeetingRoomBooking.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public sealed class BookingsController(BookingService bookingService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<BookingResponse>> CreateBooking(
        [FromBody] CreateBookingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await bookingService.CreateAsync(
            request.RoomId,
            request.Title,
            request.Start,
            request.End,
            cancellationToken);

        switch (result.Status)
        {
            case CreateBookingStatus.Created when result.Booking is { } booking:
                return StatusCode(201, new BookingResponse(
                    booking.Id,
                    booking.RoomId,
                    booking.Title,
                    booking.StartUtc,
                    booking.EndUtc));

            case CreateBookingStatus.InvalidInput:
                return Problem(statusCode: 400,
                    detail: "Provide a room, a title, and an end time after the start time.");

            case CreateBookingStatus.RoomNotFound:
                return Problem(statusCode: 404, detail: "Room not found.");

            case CreateBookingStatus.Conflict:
                return Problem(statusCode: 409,
                    detail: "The room already has a booking during this time.");

            default:
                throw new InvalidOperationException("Unexpected booking result.");
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> CancelBooking(
        int id,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return Problem(statusCode: 400, detail: "Booking ID must be positive.");
        }

        if (!await bookingService.CancelAsync(id, cancellationToken))
        {
            return Problem(statusCode: 404, detail: "Booking not found.");
        }

        return NoContent();
    }
}