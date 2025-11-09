/**
 * Sync API for Spaced Repetition
 * 
 * This endpoint handles syncing learning progress across devices.
 * Protected by password authentication.
 * 
 * SETUP:
 * 1. Start the app and try to enable sync with any password
 * 2. Check the console - it will show your password hash
 * 3. Add SYNC_PASSWORD_HASH=<your_hash> to .env file
 * 4. Restart the server
 * 5. Enable sync again with the same password
 * 
 * See SYNC_SETUP.md for detailed instructions
 */

import type { APIRoute } from 'astro';
import fs from 'fs/promises';
import path from 'path';
import crypto from 'crypto';

// Enable server-side rendering for this endpoint
export const prerender = false;

// Store sync data in a JSON file (you can change this to a database later)
const SYNC_DATA_PATH = path.join(process.cwd(), 'sync-data.json');

/**
 * Hash password for comparison
 */
function hashPassword(password: string): string {
  return crypto.createHash('sha256').update(password).digest('hex');
}

/**
 * Get password hash from environment at runtime
 */
function getPasswordHash(): string {
  return process.env.SYNC_PASSWORD_HASH || import.meta.env?.SYNC_PASSWORD_HASH || '';
}

/**
 * Verify password
 */
function verifyPassword(password: string): boolean {
  const PASSWORD_HASH = getPasswordHash();
  
  if (!PASSWORD_HASH) {
    // If no password is set, generate a hash for the provided password and log it
    const hash = hashPassword(password);
    console.log('⚠️  No SYNC_PASSWORD_HASH set in environment.');
    console.log('Add this to your .env file:');
    console.log(`SYNC_PASSWORD_HASH=${hash}`);
    return false;
  }
  return hashPassword(password) === PASSWORD_HASH;
}

/**
 * Read sync data from file
 */
async function readSyncData() {
  try {
    const data = await fs.readFile(SYNC_DATA_PATH, 'utf-8');
    return JSON.parse(data);
  } catch (error) {
    // File doesn't exist yet, return empty data
    return { data: null, lastSync: null };
  }
}

/**
 * Write sync data to file
 */
async function writeSyncData(data: any) {
  const syncData = {
    data,
    lastSync: new Date().toISOString()
  };
  await fs.writeFile(SYNC_DATA_PATH, JSON.stringify(syncData, null, 2), 'utf-8');
  return syncData;
}

/**
 * GET - Retrieve synced data
 */
export const GET: APIRoute = async ({ request }) => {
  try {
    const url = new URL(request.url);
    const password = url.searchParams.get('password');

    if (!password) {
      return new Response(JSON.stringify({ 
        error: 'Password required' 
      }), {
        status: 401,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    if (!verifyPassword(password)) {
      return new Response(JSON.stringify({ 
        error: 'Invalid password' 
      }), {
        status: 403,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    const syncData = await readSyncData();

    return new Response(JSON.stringify({
      success: true,
      data: syncData.data,
      lastSync: syncData.lastSync
    }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    });

  } catch (error) {
    console.error('Sync GET error:', error);
    return new Response(JSON.stringify({ 
      error: 'Internal server error' 
    }), {
      status: 500,
      headers: { 'Content-Type': 'application/json' }
    });
  }
};

/**
 * POST - Upload data to sync
 */
export const POST: APIRoute = async ({ request }) => {
  try {
    const body = await request.json();
    const { password, data } = body;

    if (!password) {
      return new Response(JSON.stringify({ 
        error: 'Password required' 
      }), {
        status: 401,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    if (!verifyPassword(password)) {
      return new Response(JSON.stringify({ 
        error: 'Invalid password' 
      }), {
        status: 403,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    if (!data) {
      return new Response(JSON.stringify({ 
        error: 'Data required' 
      }), {
        status: 400,
        headers: { 'Content-Type': 'application/json' }
      });
    }

    const syncData = await writeSyncData(data);

    return new Response(JSON.stringify({
      success: true,
      lastSync: syncData.lastSync
    }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' }
    });

  } catch (error) {
    console.error('Sync POST error:', error);
    return new Response(JSON.stringify({ 
      error: 'Internal server error' 
    }), {
      status: 500,
      headers: { 'Content-Type': 'application/json' }
    });
  }
};
