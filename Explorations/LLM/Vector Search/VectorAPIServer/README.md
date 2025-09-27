# Vector Memory Console (Gemini) – RAG Playground

Interactive local RAG (Retrieval-Augmented Generation) console for ingesting, embedding, summarizing, tagging, and searching documents (raw text, uploaded files, and fetched web pages) using Google Gemini embeddings. Backed by SQLite + sqlite-vec and served with Astro + Svelte.

Now includes a lightweight authentication system (email/password, admin approval, session cookies) to prevent unauthorized API usage and protect paid API keys.

## Features

-   Document, file, and web page ingestion (auto summary + tags optional)
-   Gemini embedding task type selection (RETRIEVAL_DOCUMENT, QUESTION_ANSWERING, etc.)
-   Summaries (with separate embeddings) + tag regeneration
-   Token counting per document
-   Recency re-ranking and optional summary-based retrieval
-   Distance threshold filtering
-   Re-fetch remote web sources to update content
-   Admin-gated user login & approval (session cookie + Bearer token support)

## Auth Model

1. The very first registered account (when no admin exists) is automatically: (a) marked admin, (b) auto-approved, and (c) can sign in immediately.
2. Subsequent users self‑register (email + password) and start as `pending_approval` (not admin).
3. An admin can approve / disapprove / delete users through the in-app Admin Users panel (or `/api/auth/users`).
4. Only approved users can access protected endpoints; sessions use an HttpOnly cookie (`session=...`).

### Approving Users (Admin)

List users (admin only):

```
GET /api/auth/users (Authorization: Bearer <session_token> or cookie)
```

Approve a user:

```
POST /api/auth/users { "action": "approve", "id": <userId> }
```

### Auth Endpoints

| Endpoint           | Method   | Body                | Notes                          |
| ------------------ | -------- | ------------------- | ------------------------------ |
| /api/auth/register | POST     | { email, password } | Creates unapproved user        |
| /api/auth/login    | POST     | { email, password } | Returns session cookie + token |
| /api/auth/logout   | POST     | —                   | Clears session                 |
| /api/auth/me       | GET      | —                   | Current session user (or null) |
| /api/auth/users    | GET/POST | admin only          | List or approve users          |

All other API routes now require auth: `/api/documents`, `/api/list`, `/api/search`, `/api/add-web`, `/api/embed-file`, `/api/refetch`, `/api/stats`.

## Environment Variables

```
GEMINI_API_KEY=your_api_key
VECTOR_DB_FILE=vector_memory_vec.db (optional override)
GEMINI_EMBEDDING_MODEL=gemini-embedding-001
GEMINI_EMBEDDING_DIM=768
```

## Running

```
bun install
bun run dev
```

Open http://localhost:4321 and register your first account—it becomes the initial admin automatically (no console password needed). Later registrations require admin approval before login.

## Admin Tips

-   First user = admin. To migrate admin rights later you can (for now) update the DB row (`UPDATE users SET is_admin=1 WHERE id=?`).
-   Disapproving a user prevents further API calls; deleting removes the record.

## Roadmap

-   Admin UI panel inside console (approve users)
-   PDF parsing
-   Chunking + hierarchical retrieval
-   Evaluation metrics & adaptive distance threshold
-   Tag & task filters in search
-   Optional cross-encoder / LLM re-ranker

## Security Notes

-   Sessions: simple opaque UUID + SQLite store.
-   No rate limiting yet—consider adding for exposed deployments.
-   Use reverse proxy (Caddy/Nginx) + TLS for remote access.
-   Rotate Gemini key if exposed prior to gating.

## License

Internal experimental tool (add a LICENSE file if distributing).
