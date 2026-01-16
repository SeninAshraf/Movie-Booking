using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MovieBooking.Api.Services;

namespace MovieBooking.Api.Controllers;

public class HoldRequest
{
    public List<Guid> SeatIds { get; set; } = new();
    public string UserId { get; set; } = string.Empty;
}

public class ConfirmRequest
{
    public string UserId { get; set; } = string.Empty;
}

[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpPost("{showId}/hold")]
    public async Task<IActionResult> HoldSeats(Guid showId, [FromBody] HoldRequest request)
    {
        if (request == null || request.SeatIds.Count == 0 || string.IsNullOrWhiteSpace(request.UserId))
            return BadRequest("Invalid request.");

        try
        {
            var result = await _bookingService.HoldSeatsAsync(showId, request.SeatIds, request.UserId);
            return Ok(new { Message = result });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Internal Error: " + ex.ToString() });
        }
    }

    [HttpPost("{showId}/confirm")]
    public async Task<IActionResult> ConfirmBooking(Guid showId, [FromBody] ConfirmRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.UserId))
            return BadRequest("Invalid request.");

        try
        {
            var booking = await _bookingService.ConfirmBookingAsync(showId, request.UserId);
            return CreatedAtAction(nameof(ConfirmBooking), new { id = booking.Id }, booking);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { Message = ex.Message });
        }
    }
}
