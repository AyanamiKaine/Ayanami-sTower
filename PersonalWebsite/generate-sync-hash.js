#!/usr/bin/env node

/**
 * Generate Sync Password Hash
 *
 * Quick utility to generate a password hash for the sync feature
 * Usage: node generate-sync-hash.js
 */

import crypto from "crypto";
import readline from "readline";

const rl = readline.createInterface({
    input: process.stdin,
    output: process.stdout,
});

function hashPassword(password) {
    return crypto.createHash("sha256").update(password).digest("hex");
}

console.log("\n🔐 Spaced Repetition Sync Password Hash Generator\n");

rl.question("Enter your desired sync password: ", (password) => {
    if (!password) {
        console.log("❌ Password cannot be empty");
        rl.close();
        return;
    }

    const hash = hashPassword(password);

    console.log("\n✅ Password hash generated!\n");
    console.log("Add this to your .env file:\n");
    console.log(`SYNC_PASSWORD_HASH=${hash}\n`);
    console.log("⚠️  Keep this hash secret and don't commit it to git!\n");

    rl.close();
});
