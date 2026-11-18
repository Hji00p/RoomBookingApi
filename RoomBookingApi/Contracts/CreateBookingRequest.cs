using System;
using System.ComponentModel.DataAnnotations;

namespace MeetingRoomBooking.Api.Contracts;

public sealed record CreateBookingRequest(
    [Range(1, int.MaxValue)] int RoomId,
    [Required] string? Title,
    [Required] DateTimeOffset? Start,
    [Required] DateTimeOffset? End);