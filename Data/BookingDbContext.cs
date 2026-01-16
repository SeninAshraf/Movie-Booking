using Microsoft.EntityFrameworkCore;
using MovieBooking.Api.Domain;

namespace MovieBooking.Api.Data;

public class BookingDbContext : DbContext
{
    public BookingDbContext(DbContextOptions<BookingDbContext> options) : base(options)
    {
    }

    public DbSet<Show> Shows { get; set; }
    public DbSet<Seat> Seats { get; set; }
    public DbSet<Booking> Bookings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // --- Show ---
        modelBuilder.Entity<Show>(b =>
        {
            b.HasKey(s => s.Id);
            b.Property(s => s.MovieTitle).IsRequired().HasMaxLength(200);
        });

        // --- Seat ---
        modelBuilder.Entity<Seat>(b =>
        {
            b.HasKey(s => s.Id);
            
            // Composition: Seat cannot exist without Show
            b.HasOne(s => s.Show)
             .WithMany(s => s.Seats)
             .HasForeignKey(s => s.ShowId)
             .OnDelete(DeleteBehavior.Cascade);

            // Concurrency Token (Postgres system column xmin)
            // Note: Npgsql automatically maps uint Version to xmin if we configure it
            b.Property(s => s.Version)
             .IsRowVersion();
             
            // Indexes for performance
            b.HasIndex(s => s.ShowId);
            b.HasIndex(s => new { s.ShowId, s.Row, s.Number }).IsUnique();
        });

        // --- Booking ---
        modelBuilder.Entity<Booking>(b =>
        {
            b.HasKey(x => x.Id);
            
            b.HasOne(x => x.Show)
             .WithMany()
             .HasForeignKey(x => x.ShowId);
        });
        
        // Relationship Seat -> Booking
        modelBuilder.Entity<Seat>()
            .HasOne(s => s.Booking)
            .WithMany()
            .HasForeignKey(s => s.BookingId)
            .IsRequired(false);
    }
}
