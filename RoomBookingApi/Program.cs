using MeetingRoomBooking.Application.Abstractions;
using MeetingRoomBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using MeetingRoomBooking.Application.Bookings;

Program.BuildApp(args).Run();

public partial class Program
{
    public static WebApplication BuildApp(string[] args)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ApplicationName = typeof(Program).Assembly.GetName().Name
        });

        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var connectionString = builder.Configuration.GetConnectionString("BookingDb")
            ?? throw new InvalidOperationException("Connection string 'BookingDb' is missing.");

        builder.Services.AddDbContext<AppDbContext>(
            options => options.UseNpgsql(connectionString));

        builder.Services.AddScoped<IRoomRepository, RoomRepository>();

        builder.Services.AddScoped<IBookingRepository, BookingRepository>();

        builder.Services.AddScoped<BookingService>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        return app;
    }
}
