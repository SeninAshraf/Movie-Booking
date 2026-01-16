using System;

namespace MovieBooking.Api.Domain;

public enum SeatStatus
{
    Available,
    Held,
    Booked
}

public class Seat
{
    public Guid Id { get; set; }
    public Guid ShowId { get; set; }
    public string Row { get; set; } = string.Empty;
    public int Number { get; set; }
    
    // Concurrency / Locking state
    public string? UserId { get; set; } // User holding/booking the seat
    public DateTimeOffset? HoldExpiry { get; set; }
    public Guid? BookingId { get; set; }
    
    // Optimistic Concurrency Token (Postgres xmin)
    public uint Version { get; set; }

    // Navigation
    public Show? Show { get; set; }
    public Booking? Booking { get; set; }

    // Domain Logic helper
    public SeatStatus Status
    {
        get
        {
            if (BookingId != null) return SeatStatus.Booked;
            if (HoldExpiry != null && HoldExpiry > DateTimeOffset.UtcNow) return SeatStatus.Held;
            return SeatStatus.Available;
        }
    }
    
    public bool IsAvailable() => Status == SeatStatus.Available;
}
