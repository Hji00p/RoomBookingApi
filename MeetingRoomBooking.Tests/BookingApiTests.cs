using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using MeetingRoomBooking.Api.Contracts;
using MeetingRoomBooking.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace MeetingRoomBooking.Tests;

[TestFixture]
public sealed class BookingApiTests
{
    private WebApplication? app;
    private HttpClient client = null!;
    private AppDbContext? database;

    [OneTimeSetUp]
    public async Task StartApi()
    {
        var settings = new ConfigurationBuilder()
            .AddUserSecrets<Program>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var serverConnection = settings["BOOKING_TEST_CONNECTION_STRING"]
            ?? settings.GetConnectionString("BookingDb")
            ?? throw new InvalidOperationException(
                "Set BOOKING_TEST_CONNECTION_STRING or configure BookingDb in API User Secrets.");

        var testConnection = new NpgsqlConnectionStringBuilder(serverConnection)
        {
            Database = $"booking_tests_{Guid.NewGuid():N}",
            Pooling = false
        };

        database = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(testConnection.ConnectionString)
            .Options);
        await database.Database.MigrateAsync();

        app = Program.BuildApp([
            "--environment=Testing",
            "--urls=http://127.0.0.1:0",
            $"--ConnectionStrings:BookingDb={testConnection.ConnectionString}"
        ]);
        await app.StartAsync();

        client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
    }

    [OneTimeTearDown]
    public async Task StopApi()
    {
        client?.Dispose();

        try
        {
            if (app is not null)
            {
                await app.DisposeAsync();
            }
        }
        finally
        {
            if (database is not null)
            {
                try
                {
                    await database.Database.EnsureDeletedAsync();
                }
                finally
                {
                    await database.DisposeAsync();
                }
            }
        }
    }

    [Test]
    public async Task ListsSeededRoomsAndAllowsAdjacentBookings()
    {
        var rooms = await client.GetFromJsonAsync<RoomResponse[]>("/api/rooms");
        Assert.That(rooms!.Select(room => room.Id), Does.Contain(1).And.Contain(2));

        var start = new DateTimeOffset(2030, 1, 10, 10, 0, 0, TimeSpan.Zero);
        var first = await CreateBooking(start, start.AddHours(1));
        var second = await CreateBooking(start.AddHours(1), start.AddHours(2));

        Assert.That(first.Id, Is.Positive);
        Assert.That(second.Id, Is.Not.EqualTo(first.Id));
    }

    [Test]
    public async Task RejectsConcurrentOverlappingBookings()
    {
        var start = new DateTimeOffset(2030, 1, 11, 10, 0, 0, TimeSpan.Zero);

        var responses = await Task.WhenAll(
            PostBooking(start, start.AddHours(1)),
            PostBooking(start.AddMinutes(30), start.AddHours(2)));
        using var first = responses[0];
        using var second = responses[1];

        Assert.That(responses.Select(response => response.StatusCode), Is.EquivalentTo(
            new[] { HttpStatusCode.Created, HttpStatusCode.Conflict }));
    }

    [Test]
    public async Task CancellationKeepsTheRecordAndFreesTheSlot()
    {
        var start = new DateTimeOffset(2030, 1, 12, 10, 0, 0, TimeSpan.Zero);
        var booking = await CreateBooking(start, start.AddHours(1));

        using var cancellation = await client.DeleteAsync($"/api/bookings/{booking.Id}");
        Assert.That(cancellation.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(await ReadSchedule(start), Is.Empty);

        var saved = await database!.Bookings.AsNoTracking()
            .SingleAsync(row => row.Id == booking.Id);
        Assert.That(saved.CancelledAtUtc, Is.Not.Null);

        var replacement = await CreateBooking(start, start.AddHours(1));
        Assert.That(replacement.Id, Is.Not.EqualTo(booking.Id));
    }

    [Test]
    public async Task UtcScheduleIncludesCrossMidnightBookingsInStartOrder()
    {
        var midnight = new DateTimeOffset(2030, 1, 14, 0, 0, 0, TimeSpan.Zero);
        var later = await CreateBooking(midnight.AddHours(2), midnight.AddHours(3));
        var crossing = await CreateBooking(midnight.AddMinutes(-30), midnight.AddMinutes(30));
        await CreateBooking(midnight.AddHours(-2), midnight.AddMinutes(-30));

        var schedule = await ReadSchedule(midnight);

        Assert.That(schedule.Select(booking => booking.Id),
            Is.EqualTo(new[] { crossing.Id, later.Id }));
    }

    private Task<HttpResponseMessage> PostBooking(DateTimeOffset start, DateTimeOffset end)
    {
        return client.PostAsJsonAsync("/api/bookings",
            new CreateBookingRequest(1, "Integration booking", start, end));
    }

    private async Task<BookingResponse> CreateBooking(DateTimeOffset start, DateTimeOffset end)
    {
        using var response = await PostBooking(start, end);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        return await response.Content.ReadFromJsonAsync<BookingResponse>()
            ?? throw new InvalidOperationException("The API did not return the created booking.");
    }

    private async Task<BookingResponse[]> ReadSchedule(DateTimeOffset day)
    {
        var date = day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return await client.GetFromJsonAsync<BookingResponse[]>($"/api/rooms/1/bookings?date={date}")
            ?? throw new InvalidOperationException("The API did not return a schedule.");
    }
}
