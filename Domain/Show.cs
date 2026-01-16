using System;
using System.Collections.Generic;

namespace MovieBooking.Api.Domain;

public class Show
{
    public Guid Id { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public int TotalSeats { get; set; }
    
    // Navigation property
    public ICollection<Seat> Seats { get; set; } = new List<Seat>();
}
