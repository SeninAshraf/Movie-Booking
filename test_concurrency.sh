#!/bin/bash

# Base URL
API_URL="http://localhost:5040/api"

echo "1. Fetching Show ID..."
# Fetch the first show ID from the availability endpoint of a known GUID if we had one, 
# or just query the DB. Since we don't have a 'List Shows' endpoint, we'll ask the DB or 
# use the one we found earlier.
SHOW_ID=$(psql -h localhost -U millu -d MovieBooking -t -c 'SELECT "Id" FROM "Shows" LIMIT 1;' | xargs)
echo "   Show ID: $SHOW_ID"

echo -e "\n2. Fetching Seat ID for Seat #1..."
# Get availability and parse the first seat ID (using grep/sed/awk to avoid jq dependency if missing, but assuming basic tools)
# This gets the first ID from the JSON response
SEAT_ID=$(curl -s "$API_URL/shows/$SHOW_ID/availability" | grep -oE '"id":"[a-f0-9-]+"' | head -1 | cut -d'"' -f4)
echo "   Seat ID: $SEAT_ID"

echo -e "\n3. ⚡️ STARTING CONCURRENCY WAR! ⚡️"
echo "   User A (Alice) and User B (Bob) will try to HOLD the same seat at the EXACT same time."

# Function to simulate a user holding a seat
hold_seat() {
    USER_NAME=$1
    echo "   ➡ $USER_NAME is requesting hold..."
    RESPONSE=$(curl -s -X POST "$API_URL/bookings/$SHOW_ID/hold" \
        -H "Content-Type: application/json" \
        -d "{ \"seatIds\": [\"$SEAT_ID\"], \"userId\": \"$USER_NAME\" }")
    echo "   ⬅ $USER_NAME Result: $RESPONSE"
}

# Run both in background immediately
hold_seat "Alice" &
PID_1=$!
hold_seat "Bob" &
PID_2=$!

# Wait for both to finish
wait $PID_1
wait $PID_2

echo -e "\n✅ Test Complete. One should succeed, one should fail (or fail if already held)."
