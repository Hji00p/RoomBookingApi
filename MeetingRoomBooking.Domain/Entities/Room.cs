using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeetingRoomBooking.Domain.Entities
{
    public sealed class Room
    {
        public int Id { get; private set; }
        public string Name { get; private set; } = null!;

        public Room(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Room name is required.", nameof(name));
            }

            Name = name.Trim();
        }
    }

}
