using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MovieBooking.Api.Services;

namespace MovieBooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShowsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public ShowsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpGet]
    public async Task<IActionResult> GetShows()
    {
        // Simple list for the frontend to pick from
        // In a real app, use DTOs
        var shows = await _bookingService.GetShowsAsync(); 
        return Ok(shows);
    }

    [HttpGet("{id}/availability")]
    public async Task<IActionResult> GetAvailability(Guid id)
    {
        var seats = await _bookingService.GetAvailabilityAsync(id);
        return Ok(seats);
    }
}
