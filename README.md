# Meeting Room Booking

An ASP.NET Core Web API for booking meeting rooms, backed by PostgreSQL. The optional Angular client lets users choose a room and date, view the schedule, create bookings, and cancel them.

## Prerequisites

- .NET 10 SDK for the solution commands and ASP.NET Core 8 Runtime for running the projects, which target .NET 8.
- PostgreSQL. Use a local installation or the Docker Compose service below.
- EF Core CLI for applying migrations from the terminal. Install it with `dotnet tool install --global dotnet-ef --version 8.0.2` if `dotnet ef` is unavailable.
- For the optional frontend, Node.js 22.12 or newer in the 22.x series, with npm.

## Set up PostgreSQL

Open **Manage User Secrets** for the `MeetingRoomBooking.Api` project in Visual Studio. Add the following configuration and replace the example port, username, and password with your local PostgreSQL settings. The example uses port `5433`. A default PostgreSQL installation usually uses `5432`.

```json
{
  "ConnectionStrings": {
    "BookingDb": "Host=localhost;Port=5433;Database=meeting_room_booking;Username=postgres;Password=YOUR_LOCAL_PASSWORD"
  }
}
```

You can also set the connection string from the terminal. Run this command from the directory containing `RoomBookingApi.slnx`.

```powershell
dotnet user-secrets set --project RoomBookingApi/MeetingRoomBooking.Api.csproj "ConnectionStrings:BookingDb" "Host=localhost;Port=5433;Database=meeting_room_booking;Username=postgres;Password=YOUR_LOCAL_PASSWORD"
```

From the same directory, apply the existing migration and start the API in Development mode.

```powershell
dotnet ef database update --project MeetingRoomBooking.Infrastructure --startup-project RoomBookingApi -- --environment Development
dotnet run --project RoomBookingApi/MeetingRoomBooking.Api.csproj --launch-profile https
```

The initial migration creates the schema and seeds **Room A** and **Room B**. Swagger is available at [https://localhost:7113/swagger](https://localhost:7113/swagger). If the development certificate is not trusted, run `dotnet dev-certs https --trust`.

### Optional PostgreSQL container

Start Docker Desktop and run these commands from the solution directory in PowerShell.

```powershell
$env:BOOKING_DB_PASSWORD = 'YOUR_NEW_DOCKER_PASSWORD'
docker compose up -d
docker compose ps
```

Set the API's `BookingDb` string to use `Host=127.0.0.1;Port=5434` and the same password you set for the container. Keep `Database=meeting_room_booking;Username=postgres`. Apply the migration as described above, then start the API. Compose runs PostgreSQL 18 with a persistent volume. The Docker database starts with its own data, so bookings in an existing local database are separate.

## Run the Angular frontend

Start the API with the `https` profile first. In a separate terminal opened at the solution directory, run:

```powershell
cd meeting-room-web
npm ci
npm start -- --open
```

The frontend opens at [http://localhost:4200](http://localhost:4200). Its development proxy forwards `/api` requests to `https://localhost:7113`. Dates and times on the booking screen use UTC. Select **Ends the next day** for a booking that crosses midnight.

## API

| Method and path | Success | Behavior |
| --- | --- | --- |
| `GET /api/rooms` | `200 OK` | Lists meeting rooms. |
| `GET /api/rooms/{roomId}/bookings?date=YYYY-MM-DD` | `200 OK` | Lists active bookings that overlap any part of the selected UTC day, sorted by start time. Returns an empty array when the room has no bookings for that day. |
| `POST /api/bookings` | `201 Created` | Creates a booking and returns it, including its ID. |
| `DELETE /api/bookings/{id}` | `204 No Content` | Cancels the booking and releases its slot. Repeating the request for the same booking also returns `204`. |

Invalid input returns `400 Bad Request`. A missing room or booking returns `404 Not Found`. An overlapping active booking for the same room returns `409 Conflict`. Error responses use Problem Details.

Example request body for `POST /api/bookings`:

```json
{
  "roomId": 1,
  "title": "Team planning",
  "start": "2030-01-15T10:00:00Z",
  "end": "2030-01-15T11:00:00Z"
}
```

To see the booking in the schedule, request `GET /api/rooms/1/bookings?date=2030-01-15`. The end must be after the start. Adjacent bookings are allowed, such as one ending at 11:00 and another beginning at 11:00. A booking that crosses midnight appears on both UTC dates. Cancellation preserves the database record and sets its cancellation timestamp.

## Run the tests

From the solution directory, run:

```powershell
dotnet test RoomBookingApi.slnx -c Release
```

The NUnit integration tests start the API over HTTP and apply migrations to a separate temporary PostgreSQL database. The database is removed after the test run. The tests use `BookingDb` from User Secrets by default. Set `BOOKING_TEST_CONNECTION_STRING` to use a different PostgreSQL server. The configured role must be able to create and drop the temporary test database.

The four tests cover:

1. Listing seeded rooms and accepting adjacent bookings.
2. Accepting only one of two concurrent requests for overlapping time slots in the same room.
3. Preserving the cancelled record, removing it from the schedule, and allowing its slot to be booked again.
4. Including a cross-midnight booking in the selected day's schedule and sorting by start time.

To run the Angular test, use `npm test -- --watch=false` from the `meeting-room-web` directory.

## Project structure

| Project | Responsibility |
| --- | --- |
| `RoomBookingApi` | ASP.NET Core API, HTTP contracts, controllers, and dependency registration. |
| `MeetingRoomBooking.Domain` | Room and booking entities with validation of their initial state. |
| `MeetingRoomBooking.Application` | Booking workflow and repository interfaces. |
| `MeetingRoomBooking.Infrastructure` | PostgreSQL access through EF Core, migrations, and booking transactions. |
| `MeetingRoomBooking.Tests` | NUnit HTTP integration tests. |
| `meeting-room-web` | Angular booking interface. |

## AI Prompts

1. For the `Booking` entity, validate a positive room ID, a non-blank title, and UTC times with end after start.
2. Implement `IBookingRepository` and `IRoomRepository` using EF Core. Support listing rooms, checking if a room exists, and listing, creating, and cancelling bookings. Prevent overlapping active bookings for the same room.
3. Write ASP.NET Core controllers for rooms and bookings. Add endpoints to list rooms, view a room’s bookings for a selected UTC date, and create or cancel a booking.
4. Write a service that validates booking requests, checks that the room exists, converts times to UTC, and uses repositories to create or cancel bookings. Return a conflict result when the requested time overlaps an active booking.
5. Build a single-page Angular client for the meeting room booking API. Let users select a room and date, view the UTC schedule, and create or cancel bookings. Show API errors inline and use `ngx-toastr` for success notifications.
