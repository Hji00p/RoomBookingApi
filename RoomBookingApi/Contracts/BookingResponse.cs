using System;

namespace MeetingRoomBooking.Api.Contracts;

public sealed record BookingResponse(
    int Id,
    int RoomId,
    string Title,
    DateTime StartUtc,
    DateTime EndUtc);