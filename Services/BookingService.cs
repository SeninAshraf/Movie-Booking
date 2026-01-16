using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MovieBooking.Api.Data;
using MovieBooking.Api.Domain;

namespace MovieBooking.Api.Services;

public class BookingService : IBookingService
{
    private readonly BookingDbContext _context;
    private readonly TimeSpan _holdDuration = TimeSpan.FromMinutes(2); // Short hold for testing

    public BookingService(BookingDbContext context)
    {
        _context = context;
    }

    public async Task<List<Show>> GetShowsAsync()
    {
        return await _context.Shows.ToListAsync();
    }

    public async Task<List<Seat>> GetAvailabilityAsync(Guid showId)
    {
        // Simple read, no lock needed for viewing
        return await _context.Seats
            .Where(s => s.ShowId == showId)
            .OrderBy(s => s.Row).ThenBy(s => s.Number)
            .ToListAsync();
    }

    public async Task<string> HoldSeatsAsync(Guid showId, List<Guid> seatIds, string userId)
    {
        // 1. Sort IDs to prevent deadlocks
        var sortedIds = seatIds.OrderBy(id => id).ToList();

        // 2. Start Transaction
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 3. Lock rows using raw SQL "FOR UPDATE"
            // We embed GUIDs directly to avoid 'FromSqlRaw' parameter array mapping complexities with partial queries.
            // GUIDs are safe to inject if we validate them (they are structs).
            
            var idList = string.Join(",", sortedIds.Select(id => $"'{id}'"));
            // IMPORTANT: "xmin" is a system column in Postgres and is NOT included in SELECT *
            // But EF Core expects it because we mapped it as a ConcurrencyToken.
            // We must explicitly select it.
            var sql = $"SELECT *, xmin FROM \"Seats\" WHERE \"Id\" IN ({idList}) FOR UPDATE";
            
            var seats = await _context.Seats
                .FromSqlRaw(sql)
                .Where(s => s.ShowId == showId)
                .ToListAsync();

            if (seats.Count != sortedIds.Count)
            {
                throw new InvalidOperationException("One or more seats not found.");
            }

            // 4. Validate State
            foreach (var seat in seats)
            {
                if (seat.BookingId != null)
                {
                    throw new InvalidOperationException($"Seat {seat.Row}-{seat.Number} is already booked.");
                }

                if (seat.HoldExpiry.HasValue && seat.HoldExpiry > DateTimeOffset.UtcNow)
                {
                     if (seat.UserId != userId)
                     {
                         throw new InvalidOperationException($"Seat {seat.Row}-{seat.Number} is held by another user.");
                     }
                     // If held by SAME user, we extend the hold (idempotency/refresh)
                }
            }

            // 5. Update State
            var expiry = DateTimeOffset.UtcNow.Add(_holdDuration);
            foreach (var seat in seats)
            {
                seat.HoldExpiry = expiry;
                seat.UserId = userId;
                // seat.Version will be handled by Postgres xmin automatically on update
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return "Seats held successfully.";
        }
        catch (Exception)
        {
            // Rollback happens automatically on dispose, but explicit is fine
            throw;
        }
    }

    public async Task<Booking> ConfirmBookingAsync(Guid showId, string userId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Lock seats held by this user for this show
            // Logic: Find all seats currently held by User for Show. 
            // We need to lock them to ensure they don't expire mid-confirmation.
            
            // We can't easily do "SELECT ... WHERE UserId = ... FOR UPDATE" reliably if indices change, 
            // but here we are modifying specific rows. 
            // Let's first find the IDs.
            var seatIds = await _context.Seats
                .Where(s => s.ShowId == showId && s.UserId == userId && s.BookingId == null)
                .Select(s => s.Id)
                .ToListAsync();

            if (!seatIds.Any())
            {
                 throw new InvalidOperationException("No held seats found via Confirm.");
            }
            
            // Now Lock them
            var idList = string.Join(",", seatIds.Select(id => $"'{id}'"));
            var sql = $"SELECT *, \"xmin\" FROM \"Seats\" WHERE \"Id\" IN ({idList}) FOR UPDATE";

            var seats = await _context.Seats
                .FromSqlRaw(sql)
                .ToListAsync();

            // Validate again under lock
            if (seats.Count != seatIds.Count) throw new Exception("Concurrency error fetching seats.");

            foreach (var seat in seats)
            {
                if (seat.HoldExpiry == null || seat.HoldExpiry <= DateTimeOffset.UtcNow)
                {
                    throw new InvalidOperationException("Seats hold expired.");
                }
                if (seat.BookingId != null)
                {
                    throw new InvalidOperationException("Already booked (Idempotency edge case check needed).");
                }
            }

            // Create Booking
            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                ShowId = showId,
                UserId = userId,
                ConfirmedAt = DateTimeOffset.UtcNow
            };
            
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync(); // Get Booking ID

            // Update Seats
            foreach (var seat in seats)
            {
                seat.BookingId = booking.Id;
                seat.HoldExpiry = null; // Clear hold
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return booking;
        }
        catch
        {
            throw;
        }
    }
}
