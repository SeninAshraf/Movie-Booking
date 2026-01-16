using Microsoft.EntityFrameworkCore;
using MovieBooking.Api.Data;
using MovieBooking.Api.Services;
using MovieBooking.Api.Workers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Data Access
builder.Services.AddDbContext<BookingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Domain Services
builder.Services.AddScoped<IBookingService, BookingService>();

// Background Workers
builder.Services.AddHostedService<SeatCleanupWorker>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();



// Simple Seeding for testing
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
    // No async in top-level implicit main without await, but simple check is fine
    if (!context.Shows.Any())
    {
        var showId = Guid.NewGuid();
        context.Shows.Add(new MovieBooking.Api.Domain.Show 
        { 
            Id = showId, 
            MovieTitle = "Inception", 
            StartTime = DateTimeOffset.UtcNow.AddHours(2),
            TotalSeats = 10 
        });

        // Add 10 seats
        for (int i = 1; i <= 10; i++)
        {
            context.Seats.Add(new MovieBooking.Api.Domain.Seat 
            { 
                Id = Guid.NewGuid(), 
                ShowId = showId, 
                Row = "A", 
                Number = i 
            });
        }
        context.SaveChanges();
    }
}

app.Run();

