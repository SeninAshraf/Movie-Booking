# 🍿 Baby-Steps Guide to Your Movie Booking System

Hello! Let's understand this project from the very beginning. Imagine we are building a system for a cinema hall.

## 1. The Big Problem: "The Last Seat"

Imagine there is **only 1 seat left** for a new Spiderman movie.
- **Alice** wants it.
- **Bob** wants it.

Both of them click **"BOOK"** at the *exact same time*.
If our computer is dumb, it might tell Alice "Yes!", and tell Bob "Yes!".
Now, two people show up for one seat. Chaos! 🥊

**Our Goal:** Build a referee that guarantees only *one* person gets the seat, no matter what.

---

## 2. The Parts of Our System

We built three main parts:

### 🏠 The Database (PostgreSQL)
Think of this as **The Master Record Book**. It is a real book in a safe. It lists every seat and who owns it.
- **Rule:** You cannot scribble in this book. You must ask the Librarian (The API) to write for you.

### 🤖 The API (Backend / .NET)
This is **The Librarian**.
- Alice sends a message: "I want Seat 5."
- The Librarian says: "Hold on."
- The Librarian runs to the safe, **locks the door**, checks if Seat 5 is free, writes "Alice", and then unlocks the door.
- If Bob tries to enter while the door is locked, he has to wait outside.

### 🖥️ The Frontend (HTML Website)
This is **The Menu**.
- It just shows what the Librarian tells it. green seats are free, red seats are taken.

---

## 3. How We Solved The "Fight" (The Code)

We used a magic trick called **"Locking"** (`SELECT FOR UPDATE`).

**In `BookingService.cs`:**
```csharp
// 1. Start a Transaction (Open the safe)
await transaction.BeginAsync();

// 2. Lock the Seat (Guard the specific row in the book)
// If Bob comes here while Alice is here, Bob STOPS and waits.
var sql = "SELECT * FROM Seats WHERE Id = '...' FOR UPDATE";

// 3. Mark it as "Held"
seat.UserId = "Alice";
seat.HoldExpiry = "In 2 Minutes";

// 4. Commit (Close the safe and unlock)
await transaction.CommitAsync();
```

---

## 4. Let's Play With It!

### Step 1: Start the Engine
Open your terminal (the black box) and type:
```bash
./.dotnet/dotnet run
```
This starts the Librarian (API).

### Step 2: Open the Website
Go to your browser (Chrome/Safari) and typ:
`http://localhost:5040`

### Step 3: Be Two People
1. Open the website in **Tab 1**. Let's say this is Alice.
2. Open the website in **Tab 2**. Let's say this is Bob.

### Step 4: The Fight
1. Alice (Tab 1) clicks on **Seat 1**. Click "Hold".
   - It turns **Yellow**.
2. Quickly go to Bob (Tab 2).
   - Look! It turned Yellow for him too!
3. Try to click "Hold" for Bob on the same seat.
   - **Computer says NO!** "Seat held by another user."

You just proved the referee works! 🎉

---

## 5. What happens to seats when a booking is NOT completed?

This is a very important question! 
Imagine Alice holds a seat. It turns **Yellow** (Held). Ideally, she pays and it turns **Red** (Booked).

**But what if:**
- Alice's internet disconnects? 🔌
- Alice closes her browser window? ❌
- Alice just walks away to make a sandwich? 🥪

**The Problem:** The seat is "Held" (Yellow). No one else can buy it. If we do nothing, that seat stays empty forever. The cinema loses money! 💸

**The Solution: The "Cleanup Worker" 🧹**
We built a robot (background service) that lives inside our API.
1.  **Every 5 seconds**, this robot wakes up.
2.  It checks the list of "Held" seats.
3.  It asks: *"Has this seat been held for more than 2 minutes?"*
4.  If the answer is **YES**, the robot says: *"Time's up! Alice didn't pay."*
5.  The robot **erases the hold**. The seat turns **Green (Available)** again.

So, if a booking is not completed, **the system automatically recycles the seat** so someone else can buy it. No seat is ever wasted.

---

## Summary
You built a **smart cinema system** that:
1. **Never oversells tickets** (The Locking).
2. **Handles waiting lines** (Concurrency).
3. **Cleans up** after people who leave (Background Worker).

You are now a Backend Engineer! 🚀
