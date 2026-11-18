using MeetingRoomBooking.Application.Abstractions;
using MeetingRoomBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MeetingRoomBooking.Infrastructure.Persistence;

public sealed class RoomRepository(AppDbContext db) : IRoomRepository
{
    public async Task<IReadOnlyList<Room>> ListAsync(
        CancellationToken cancellationToken)
    {
        return await db.Rooms
            .AsNoTracking()
            .OrderBy(room => room.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(int roomId, CancellationToken cancellationToken)
    {
        return db.Rooms.AnyAsync(room => room.Id == roomId, cancellationToken);
    }
}