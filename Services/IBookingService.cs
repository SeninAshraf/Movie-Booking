using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MovieBooking.Api.Domain;

namespace MovieBooking.Api.Services;

public interface IBookingService
{
    Task<List<Show>> GetShowsAsync();
    Task<List<Seat>> GetAvailabilityAsync(Guid showId);
    Task<string> HoldSeatsAsync(Guid showId, List<Guid> seatIds, string userId);
    Task<Booking> ConfirmBookingAsync(Guid showId, string userId);
}
