using System;

namespace MovieBooking.Api.Domain;

public class Booking
{
    public Guid Id { get; set; }
    public Guid ShowId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTimeOffset ConfirmedAt { get; set; }
    
    // Navigation
    public Show? Show { get; set; }
}
