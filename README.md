# Movie Booking Engine

A robust, concurrency-safe backend system for managing movie seat availability and bookings.
Designed to handle high contention, network failures, and race conditions without overbooking.


https://github.com/SeninAshraf/Movie-Booking/assets/movie.mov


## The Problem
We need to ensure that when 1,000 users try to book the last seat for "Avengers" at the exact same millisecond:
1. Only **one** user succeeds.
2. The other 999 are told the seat is taken.
3. If the winner's connection drops before payment, the seat eventually becomes free again.
4. The system state remains consistent even if the server crashes mid-transaction.

## The Solution

### Architecture
- **Language**: C# (.NET 8)
- **Database**: PostgreSQL
- **ORM**: Entity Framework Core

### Concurrency Strategy: Pessimistic Locking
We intentionally avoid "application-level locks" (like Redis locks or in-memory mutexes) because they are fragile in distributed environments or across server restarts.

Instead, we rely on the database as the single source of truth using **Row-Level Locking** (`SELECT FOR UPDATE`).

**How it works:**
1. **Hold Request**: When a user selects seats, we open a transaction and attempt to lock the specific seat rows.
   ```sql
   SELECT * FROM "Seats" WHERE "Id" IN (...) FOR UPDATE;
   ```
2. **Lock Acquisition**:
   - If User A gets the lock, User B's request physically waits in the database queue.
   - Once User A updates the `HoldExpiry` and commits, User B's query executes, sees the new expiry, and fails the validation.
3. **Transparency**: The database handles the queueing. We don't need complex distributed lock managers.

### Handling Edge Cases
- **Abandoned Bookings**: A background worker (`SeatCleanupWorker`) scans every few seconds for seats where `HoldExpiry < Now` and `BookingId IS NULL`. It atomically releases them.
- **Server Crashes**: Since state is persisted in Postgres, a server restart doesn't lose lock information (expiration is a timestamp, not a memory timer).
- **Idempotency**: The `ConfirmBooking` endpoint checks if the seat is *already* booked by the requesting user. If they retry the request (refresh page), they get the same success response without error.

## Database Schema

- **Show**: Metadata (Title, Time).
- **Seat**: The core state machine.
  - `Status`: Derived from `BookingId` (Booked) -> `HoldExpiry` (Held) -> Null (Available).
  - `Version`: Postgres `xmin` token for optimistic concurrency sanity checks (though we rely on pessimistic locks).
- **Booking**: An immutable record of a confirmed sale.

## Running the Project

**Prerequisites**: Docker (for Postgres) or a local Postgres instance, and .NET 8 SDK.

1. **Start Database**
   ```bash
   # If you have Docker
   docker run -e POSTGRES_PASSWORD=postgres -p 5432:5432 -d postgres
   ```
   *Or use your local Postgres connection string in `appsettings.json`.*

2. **Run Migrations**
   ```bash
   dotnet ef database update
   ```

3. **Start API**
   ```bash
   dotnet run
   ```
   The app seeds a demo show ("Inception") automatically on first run.

4. **Verify**
   - Check the **Swagger UI**: `http://localhost:5040/swagger`
   - Use the **included Frontend**: `http://localhost:5040/` (Simple HTML/JS visualization)

## Testing Concurrency
Included is a script `test_concurrency.sh` that spawns simulated users to fight for the same seat instantly.
```bash
./test_concurrency.sh
```

## Design Limitations
- **Scalability**: `SELECT FOR UPDATE` reduces throughput strictly for *contested* rows. For a single show with 10k seats, this is fine. For a system selling 1M tickets/sec, we'd move to an inventory decrement model or shards.
- **No Auth**: User IDs are string placeholders as per requirements.
