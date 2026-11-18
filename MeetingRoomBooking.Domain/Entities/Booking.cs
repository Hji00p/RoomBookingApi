using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeetingRoomBooking.Domain.Entities
{
    public sealed class Booking
    {
        public int Id { get; private set; }
        public int RoomId { get; private set; }
        public string Title { get; private set; }
        public DateTime StartUtc { get; private set; }
        public DateTime EndUtc { get; private set; }
        public DateTime? CancelledAtUtc { get; private set; }

        public Booking(int roomId, string title, DateTime startUtc, DateTime endUtc)
        {
            if (roomId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(roomId), "Room ID must be positive.");
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("Meeting title is required.", nameof(title));
            }

            EnsureUtc(startUtc, nameof(startUtc));
            EnsureUtc(endUtc, nameof(endUtc));

            if (endUtc <= startUtc)
            {
                throw new ArgumentException(
                    "End time must be after start time.", nameof(endUtc));
            }

            RoomId = roomId;
            Title = title.Trim();
            StartUtc = startUtc;
            EndUtc = endUtc;
        }

        private static void EnsureUtc(DateTime value, string parameterName)
        {
            if (value.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("Time must be in UTC.", parameterName);
            }
        }
    }
}
