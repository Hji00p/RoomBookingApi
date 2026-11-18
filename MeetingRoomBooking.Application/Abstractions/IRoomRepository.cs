using MeetingRoomBooking.Domain.Entities;

namespace MeetingRoomBooking.Application.Abstractions;

public interface IRoomRepository
{
    Task<IReadOnlyList<Room>> ListAsync(CancellationToken cancellationToken);
    Task<bool> ExistsAsync(int roomId, CancellationToken cancellationToken);
}