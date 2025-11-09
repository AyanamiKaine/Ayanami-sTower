# Spaced Repetition Sync Setup

## Overview

The sync feature allows you to backup and synchronize your spaced repetition learning progress across multiple devices using a password-protected API.

## Features

-   🔒 Password-protected sync (only you can access your data)
-   ☁️ Automatic background sync after each review
-   🔄 Manual sync on demand
-   📱 Works across all your devices (desktop, mobile, tablet)
-   🚫 Other users who visit your site use local storage only

## Setup Instructions

### 1. Generate Your Password Hash

**Option A: Use the helper script (Recommended)**

```bash
node generate-sync-hash.js
```

Enter your desired password, and it will generate the hash for you!

**Option B: Generate via the app**

1. Choose a strong password (this will be your sync password)
2. Start your dev server: `bun run dev`
3. Open the spaced repetition app
4. Click the "☁️ Sync" button
5. Enter your desired password and click "Enable Sync"
6. Check the terminal/console - it will show your password hash
7. Copy the hash

### 2. Configure Environment Variable

Create a `.env` file in the project root:

```bash
cp .env.example .env
```

Edit the `.env` file and add your password hash:

```env
SYNC_PASSWORD_HASH=your_generated_hash_here
```

### 3. Restart Your Server

Restart your dev/production server to load the environment variable:

```bash
bun run dev
```

### 4. Enable Sync on Your Devices

On each device where you want to sync:

1. Open the spaced repetition app
2. Click the "☁️ Sync" button
3. Enter your sync password (the same one you used to generate the hash)
4. Click "Enable Sync"

Your progress will now sync automatically!

## How It Works

### First-Time Setup

1. You enter your password on your first device
2. The app syncs your current local data to the server
3. Password is stored locally (in localStorage) so you don't need to re-enter it

### On Other Devices

1. You enter the same password
2. The app downloads your synced data from the server
3. Your progress is now synchronized

### During Reviews

-   After each card review, your progress is automatically synced to the server in the background
-   No user action needed!

### Manual Sync

-   Click "☁️ Sync" button to open sync settings
-   Click "🔄 Sync Now" to force an immediate sync from the server

## Security

-   Your password is hashed using SHA-256
-   The password is never stored on the server
-   Only the hash is used for verification
-   Data is stored in a JSON file on your server (you can change this to a database if needed)

## Storage Location

By default, sync data is stored in:

```
PersonalWebsite/sync-data.json
```

You can change this location in `src/pages/api/sync.json.ts` by modifying the `SYNC_DATA_PATH` variable.

## Migrating to a Database (Optional)

If you want to use a database instead of a JSON file:

1. Install a database client (e.g., Prisma, Drizzle)
2. Update `src/pages/api/sync.json.ts`:
    - Replace `readSyncData()` to query your database
    - Replace `writeSyncData()` to update your database
3. Keep the password authentication logic unchanged

## Troubleshooting

### "Invalid password" error

-   Make sure your `.env` file has the correct hash
-   Restart your server after adding/changing the `.env` file
-   Verify you're using the same password that generated the hash

### Sync not working

-   Check the browser console for errors
-   Check the server logs for API errors
-   Make sure the sync API endpoint is accessible at `/api/sync.json`

### Lost password

1. Delete the `.env` file's `SYNC_PASSWORD_HASH`
2. Choose a new password
3. Follow the "Generate Your Password Hash" steps again

## Disabling Sync

To disable sync on a device:

1. Click "☁️ Synced" button
2. Click "Disable Sync"
3. Your local data remains intact, but won't sync anymore

## For Other Users

Other people who visit your website:

-   Will NOT see the sync feature
-   Will use localStorage only
-   Their data stays on their device
-   They cannot access your synced data

This is your personal sync feature! 🎉
