# ActivePieces

Single-host rootless-podman deployment of ActivePieces, fronted by the system nginx
and Letsencrypt at `https://activepieces.ayanamikaine.com`.

## Layout

| Component | Where |
|---|---|
| Orchestrator | [`deploy.ts`](deploy.ts) — Bun TypeScript |
| nginx site config | [`nginx/activepieces.conf`](nginx/activepieces.conf) — installed to `/etc/nginx/conf.d/` |
| Secrets | `~/activepieces/.env` (mode 0600, **outside this repo**) |
| Container data | podman volumes `activepieces-postgres-data`, `activepieces-cache` |
| Autostart | systemd `--user` units in `~/.config/systemd/user/` |

## First-time install

```bash
cd Applications/ActivePieces
bun deploy.ts
```

The script is idempotent — re-run it after pulling updates. It:

1. Generates `~/activepieces/.env` if missing (encryption key, JWT secret, postgres password).
2. Pulls postgres, redis, and ActivePieces images.
3. Creates the podman network and named volumes.
4. Starts the three containers in order, waiting for postgres readiness before launching the app.
5. Writes systemd `--user` unit files and enables them so the stack survives reboot.

## After the first run — exposing publicly

```bash
sudo cp nginx/activepieces.conf /etc/nginx/conf.d/
sudo nginx -t && sudo systemctl reload nginx
sudo certbot --nginx -d activepieces.ayanamikaine.com
```

Certbot rewrites `activepieces.conf` in place to add the HTTPS server block.
The DNS A record must already point at this server **without** Cloudflare proxy
(gray cloud) for the HTTP-01 challenge — flip back to orange (Proxied) afterwards.

## Operations cheatsheet

```bash
# logs
podman logs -f activepieces

# restart just the app (preserves DB/redis state)
systemctl --user restart activepieces.service

# upgrade to latest image
podman pull ghcr.io/activepieces/activepieces:latest
bun deploy.ts                         # recreates the activepieces container

# back up DB
podman exec activepieces-postgres pg_dump -U postgres activepieces > backup.sql
```

## Why these choices

- **Rootless podman, no docker-compose**: matches the existing `my-personal-website` /
  `stella-wiki` pattern on this server. No new daemon or compose dependency.
- **Loopback bind**: the app listens on `127.0.0.1:8090`. The system nginx is the
  only public ingress, so TLS, rate limits, and access logs live in one place.
- **`AP_EXECUTION_MODE=UNSANDBOXED`**: the sandboxed mode wants Docker socket access
  to spawn isolated piece runners, which doesn't fit a rootless podman host.
  Personal/single-tenant use case so the trade-off is fine.
- **Secrets outside the repo**: `~/activepieces/.env` (mode 0600) is generated on
  first run and never committed. Re-running `deploy.ts` reads existing values and
  only generates ones that are missing — important because rotating
  `AP_ENCRYPTION_KEY` would invalidate every encrypted DB row.
