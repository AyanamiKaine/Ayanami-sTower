#!/usr/bin/env bun
/**
 * ActivePieces deploy orchestrator (rootless podman, single host).
 *
 * What it does, idempotently:
 *   1. Generates secrets at ~/activepieces/.env on first run.
 *   2. Pulls images (postgres:14-alpine, redis:7-alpine, activepieces).
 *   3. Creates the podman network + named volumes.
 *   4. Starts postgres, redis, activepieces (in that order, with health waits).
 *   5. Installs systemd --user units so the stack survives reboot.
 *
 * Public exposure (nginx + Letsencrypt) is handled outside this script —
 * see the matching nginx/activepieces.conf and the README.
 *
 * Re-run safe: every podman/systemd command tolerates "already exists",
 * so this can be invoked repeatedly to converge on the desired state.
 */

import { $ } from "bun";
import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { homedir } from "node:os";
import { join } from "node:path";
import { randomBytes } from "node:crypto";

// ---------- Configuration ----------

const STATE_DIR = join(homedir(), "activepieces");
const ENV_FILE = join(STATE_DIR, ".env");

const NETWORK = "activepieces-net";
const VOLUME_PG = "activepieces-postgres-data";
const VOLUME_CACHE = "activepieces-cache";

const C_POSTGRES = "activepieces-postgres";
const C_REDIS = "activepieces-redis";
const C_APP = "activepieces";

const IMG_POSTGRES = "docker.io/library/postgres:14-alpine";
const IMG_REDIS = "docker.io/library/redis:7-alpine";
const IMG_APP = "ghcr.io/activepieces/activepieces:latest";

// Bind only to loopback — nginx terminates TLS and forwards.
const APP_BIND = "127.0.0.1:8090";

const FRONTEND_URL = "https://activepieces.ayanamikaine.com";

const SYSTEMD_USER_DIR = join(homedir(), ".config", "systemd", "user");

// ---------- Helpers ----------

const log = (msg: string) => console.log(`[deploy] ${msg}`);
const warn = (msg: string) => console.error(`[deploy] WARN: ${msg}`);

/** Run a command, return stdout. Throws on non-zero exit. */
async function run(cmd: string[], opts: { ignoreError?: boolean } = {}) {
  const proc = Bun.spawn(cmd, {
    stdout: "pipe",
    stderr: "pipe",
  });
  const [stdout, stderr] = await Promise.all([
    new Response(proc.stdout).text(),
    new Response(proc.stderr).text(),
  ]);
  const exitCode = await proc.exited;
  if (exitCode !== 0 && !opts.ignoreError) {
    throw new Error(
      `command failed (${exitCode}): ${cmd.join(" ")}\n${stderr || stdout}`,
    );
  }
  return { stdout, stderr, exitCode };
}

/** True if a container with this name exists (running or stopped). */
async function containerExists(name: string) {
  const { stdout } = await run([
    "podman", "ps", "-a", "--format", "{{.Names}}", "--filter", `name=^${name}$`,
  ]);
  return stdout.trim().split("\n").some((l) => l === name);
}

async function networkExists(name: string) {
  const { exitCode } = await run(["podman", "network", "exists", name], { ignoreError: true });
  return exitCode === 0;
}

async function volumeExists(name: string) {
  const { exitCode } = await run(["podman", "volume", "exists", name], { ignoreError: true });
  return exitCode === 0;
}

// ---------- Steps ----------

function ensureEnv(): Record<string, string> {
  if (!existsSync(STATE_DIR)) {
    mkdirSync(STATE_DIR, { recursive: true, mode: 0o700 });
  }

  let env: Record<string, string> = {};

  if (existsSync(ENV_FILE)) {
    log(`reading existing secrets from ${ENV_FILE}`);
    const raw = readFileSync(ENV_FILE, "utf8");
    for (const line of raw.split("\n")) {
      const m = line.match(/^([A-Z_][A-Z0-9_]*)=(.*)$/);
      if (m) env[m[1]] = m[2];
    }
  }

  // Generate any missing secret. Once written, never regenerate — that would
  // invalidate encrypted DB rows and force a re-auth across all users.
  const need = (key: string, gen: () => string) => {
    if (!env[key]) {
      env[key] = gen();
      log(`generated ${key}`);
    }
  };

  need("AP_ENCRYPTION_KEY", () => randomBytes(16).toString("hex")); // 32 hex chars = 16 bytes
  need("AP_JWT_SECRET", () => randomBytes(32).toString("hex"));     // 64 hex chars
  need("AP_POSTGRES_PASSWORD", () => randomBytes(24).toString("base64url"));

  // Non-secret defaults — overwritten only if missing.
  const defaults: Record<string, string> = {
    AP_FRONTEND_URL: FRONTEND_URL,
    AP_DB_TYPE: "POSTGRES",
    AP_POSTGRES_DATABASE: "activepieces",
    AP_POSTGRES_HOST: C_POSTGRES,
    AP_POSTGRES_PORT: "5432",
    AP_POSTGRES_USERNAME: "postgres",
    AP_REDIS_HOST: C_REDIS,
    AP_REDIS_PORT: "6379",
    AP_QUEUE_MODE: "REDIS",
    AP_EXECUTION_MODE: "UNSANDBOXED", // no Docker-in-Docker on this host
    AP_TELEMETRY_ENABLED: "false",
    AP_TRIGGER_DEFAULT_POLL_INTERVAL: "5",
    AP_WEBHOOK_TIMEOUT_SECONDS: "30",
  };
  for (const [k, v] of Object.entries(defaults)) {
    if (!env[k]) env[k] = v;
  }

  // Persist (mode 0600 — secrets).
  const body = Object.entries(env)
    .map(([k, v]) => `${k}=${v}`)
    .join("\n") + "\n";
  writeFileSync(ENV_FILE, body, { mode: 0o600 });
  log(`wrote ${ENV_FILE}`);

  return env;
}

async function pullImages() {
  for (const img of [IMG_POSTGRES, IMG_REDIS, IMG_APP]) {
    log(`pulling ${img}`);
    await run(["podman", "pull", img]);
  }
}

async function ensureNetworkAndVolumes() {
  if (!(await networkExists(NETWORK))) {
    log(`creating network ${NETWORK}`);
    await run(["podman", "network", "create", NETWORK]);
  }
  for (const v of [VOLUME_PG, VOLUME_CACHE]) {
    if (!(await volumeExists(v))) {
      log(`creating volume ${v}`);
      await run(["podman", "volume", "create", v]);
    }
  }
}

async function startPostgres(env: Record<string, string>) {
  if (await containerExists(C_POSTGRES)) {
    log(`${C_POSTGRES} already exists; ensuring it's running`);
    await run(["podman", "start", C_POSTGRES], { ignoreError: true });
  } else {
    log(`starting ${C_POSTGRES}`);
    await run([
      "podman", "run", "-d",
      "--name", C_POSTGRES,
      "--network", NETWORK,
      "--restart", "unless-stopped",
      "-v", `${VOLUME_PG}:/var/lib/postgresql/data`,
      "-e", `POSTGRES_DB=${env.AP_POSTGRES_DATABASE}`,
      "-e", `POSTGRES_USER=${env.AP_POSTGRES_USERNAME}`,
      "-e", `POSTGRES_PASSWORD=${env.AP_POSTGRES_PASSWORD}`,
      IMG_POSTGRES,
    ]);
  }

  // Wait for postgres to accept connections.
  log("waiting for postgres readiness…");
  for (let i = 0; i < 60; i++) {
    const { exitCode } = await run([
      "podman", "exec", C_POSTGRES,
      "pg_isready", "-U", env.AP_POSTGRES_USERNAME, "-d", env.AP_POSTGRES_DATABASE,
    ], { ignoreError: true });
    if (exitCode === 0) {
      log("postgres is ready");
      return;
    }
    await Bun.sleep(1000);
  }
  throw new Error("postgres did not become ready within 60s");
}

async function startRedis() {
  if (await containerExists(C_REDIS)) {
    log(`${C_REDIS} already exists; ensuring it's running`);
    await run(["podman", "start", C_REDIS], { ignoreError: true });
    return;
  }
  log(`starting ${C_REDIS}`);
  await run([
    "podman", "run", "-d",
    "--name", C_REDIS,
    "--network", NETWORK,
    "--restart", "unless-stopped",
    IMG_REDIS,
  ]);
}

async function startApp(env: Record<string, string>) {
  if (await containerExists(C_APP)) {
    log(`${C_APP} already exists; recreating to pick up env changes`);
    await run(["podman", "rm", "-f", C_APP], { ignoreError: true });
  }
  log(`starting ${C_APP} on ${APP_BIND}`);

  // Build the env-var args from the env file.
  const envArgs: string[] = [];
  for (const [k, v] of Object.entries(env)) {
    envArgs.push("-e", `${k}=${v}`);
  }

  await run([
    "podman", "run", "-d",
    "--name", C_APP,
    "--network", NETWORK,
    "--restart", "unless-stopped",
    "-p", `${APP_BIND}:80`,
    "-v", `${VOLUME_CACHE}:/usr/src/app/cache`,
    ...envArgs,
    IMG_APP,
  ]);
}

async function waitForAppHealthy() {
  log("waiting for activepieces HTTP 200…");
  // First boot does DB migrations; can take a couple of minutes on a slow disk.
  const deadline = Date.now() + 5 * 60 * 1000;
  while (Date.now() < deadline) {
    try {
      const res = await fetch(`http://${APP_BIND}/`, { signal: AbortSignal.timeout(5000) });
      if (res.ok || res.status === 302) {
        log(`activepieces is up (HTTP ${res.status})`);
        return;
      }
    } catch {
      // not up yet
    }
    await Bun.sleep(2000);
  }
  warn("activepieces did not return 200/302 within 5min — check `podman logs activepieces`.");
}

async function installSystemdUnits() {
  // Mirrors how my-personal-website-green.service is registered:
  // a transient unit per container so `loginctl enable-linger` keeps them up.
  if (!existsSync(SYSTEMD_USER_DIR)) {
    mkdirSync(SYSTEMD_USER_DIR, { recursive: true });
  }

  const writeUnit = (name: string, body: string) => {
    const path = join(SYSTEMD_USER_DIR, `${name}.service`);
    writeFileSync(path, body);
    log(`wrote ${path}`);
  };

  // postgres
  writeUnit(C_POSTGRES, `[Unit]
Description=ActivePieces PostgreSQL (podman)
After=network-online.target
Wants=network-online.target

[Service]
Type=simple
Restart=on-failure
RestartSec=10
ExecStart=/usr/bin/podman start -a ${C_POSTGRES}
ExecStop=/usr/bin/podman stop -t 10 ${C_POSTGRES}

[Install]
WantedBy=default.target
`);

  // redis
  writeUnit(C_REDIS, `[Unit]
Description=ActivePieces Redis (podman)
After=network-online.target
Wants=network-online.target

[Service]
Type=simple
Restart=on-failure
RestartSec=10
ExecStart=/usr/bin/podman start -a ${C_REDIS}
ExecStop=/usr/bin/podman stop -t 10 ${C_REDIS}

[Install]
WantedBy=default.target
`);

  // activepieces app
  writeUnit(C_APP, `[Unit]
Description=ActivePieces app (podman)
After=network-online.target ${C_POSTGRES}.service ${C_REDIS}.service
Wants=network-online.target
Requires=${C_POSTGRES}.service ${C_REDIS}.service

[Service]
Type=simple
Restart=on-failure
RestartSec=10
ExecStart=/usr/bin/podman start -a ${C_APP}
ExecStop=/usr/bin/podman stop -t 30 ${C_APP}

[Install]
WantedBy=default.target
`);

  await run(["systemctl", "--user", "daemon-reload"]);
  for (const name of [C_POSTGRES, C_REDIS, C_APP]) {
    await run(["systemctl", "--user", "enable", `${name}.service`]);
  }

  // enable-linger so user services run at boot before any login session.
  // sudo is a no-op for the parent if already lingering — that's fine.
  await run(["sudo", "-n", "loginctl", "enable-linger", "ayanami"], { ignoreError: true });
}

// ---------- Main ----------

async function main() {
  log("=== ActivePieces deploy starting ===");
  const env = ensureEnv();

  await pullImages();
  await ensureNetworkAndVolumes();
  await startPostgres(env);
  await startRedis();
  await startApp(env);
  await waitForAppHealthy();
  await installSystemdUnits();

  log("=== Deploy complete ===");
  log(`Internal: http://${APP_BIND}`);
  log(`Public  : ${FRONTEND_URL} (after nginx + certbot)`);
}

main().catch((err) => {
  console.error("\n[deploy] FAILED:", err.message);
  process.exit(1);
});
