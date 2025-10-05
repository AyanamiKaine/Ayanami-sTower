"""Vector Embeddings REST API Server

Provides a thin HTTP layer over the local sqlite-vec database used by the
existing scripts (VectorSearchExample.py and markdown_vault_indexer.py).

Goals:
- Decouple embedding storage (SQLite + sqlite-vec) from clients.
- Offer CRUD & search endpoints for documents/chunks.
- Expose recency-aware re-ranking similar to existing CLI.
- Optional Gemini-assisted query rewriting (server-side) to improve retrieval.

This is a minimal MVP; security, auth, rate limiting and multi-tenant concerns
are intentionally out of scope for the initial version.

Run (development):
  uvicorn vector_api_server:app --reload --port 8000

Example Usage (after starting server):
  curl -X POST http://localhost:8000/search -H "Content-Type: application/json" \
       -d '{"query":"What is the energy source for a cell?", "top_k":3}'

Environment:
  EMBEDDING_MODEL_NAME   (optional) sentence-transformers model name
  GEMINI_API_KEY / GOOGLE_API_KEY (optional) for query rewriting if enabled

Database:
  By default uses vector_memory_vec.db (same as prior script) or override with --db.

"""
from __future__ import annotations

import os
import math
import time
import sqlite3
import threading
from typing import List, Optional, Sequence, Iterable, TYPE_CHECKING, Any
from dataclasses import dataclass

import numpy as np
from fastapi import FastAPI, HTTPException, Query, Body
from pydantic import BaseModel, Field

# Attempt to import sentence-transformers lazily.
try:
    from sentence_transformers import SentenceTransformer  # type: ignore
except Exception:  # pragma: no cover
    SentenceTransformer = None  # type: ignore

# Gemini optional
try:  # Prefer new google-genai package name 'google-genai'; fall back if installed as 'google'
    from google import genai  # type: ignore
    from google.genai import types as genai_types  # type: ignore
except Exception:  # pragma: no cover
    genai = None  # type: ignore
    genai_types = None  # type: ignore

if TYPE_CHECKING:  # pragma: no cover - for type hints only
    from sentence_transformers import SentenceTransformer as _SentenceTransformerType  # noqa: F401

DB_FILE = os.getenv("VECTOR_DB_FILE", "vector_memory_vec.db")

# Embedding backend selection: "local" (sentence-transformers) or "gemini"
EMBEDDING_BACKEND = os.getenv("EMBEDDING_BACKEND", "local").lower()

# Local ST model name
EMBEDDING_MODEL_NAME = os.getenv("EMBEDDING_MODEL_NAME", "all-MiniLM-L6-v2")

# Gemini settings
GEMINI_MODEL_NAME = os.getenv("GEMINI_MODEL_NAME", "gemini-2.0-flash-001")
GEMINI_EMBEDDING_MODEL = os.getenv("GEMINI_EMBEDDING_MODEL", "gemini-embedding-001")
GEMINI_EMBEDDING_DIM = int(os.getenv("GEMINI_EMBEDDING_DIM", "768"))  # Recommended: 768|1536|3072
GEMINI_TASK_DOC = os.getenv("GEMINI_TASK_DOC", "RETRIEVAL_DOCUMENT")
GEMINI_TASK_QUERY = os.getenv("GEMINI_TASK_QUERY", "RETRIEVAL_QUERY")
GOOGLE_API_KEY = os.getenv("GOOGLE_API_KEY") or os.getenv("GEMINI_API_KEY") or os.getenv("GEMINI_API")

app = FastAPI(title="Vector Embeddings API", version="0.1.0")

_embedding_model: Optional[Any] = None
_gemini_client = None
_conn: Optional[sqlite3.Connection] = None
_db_lock = threading.Lock()


# ---------------------------------------------------------------------------
# Data Models
# ---------------------------------------------------------------------------

class DocCreate(BaseModel):
    text: str = Field(..., description="Raw document text to embed and store")
    id: Optional[int] = Field(None, description="Optional explicit ID; if omitted, auto increment")

class DocUpdate(BaseModel):
    text: str

class SearchRequest(BaseModel):
    query: str
    top_k: int = 5
    rewrite_mode: str = Field("none", description="none|summarize|expand|disambiguate|auto")
    show_rewrite: bool = False
    use_recency: bool = False
    recency_half_life: float = 7 * 24 * 3600.0
    recency_alpha: float = 0.3

class SearchResultDoc(BaseModel):
    id: int
    distance: float
    text: str
    created_at: Optional[int] = None
    updated_at: Optional[int] = None

class SearchResponse(BaseModel):
    query: str
    rewritten_query: str
    top_k: int
    results: List[SearchResultDoc]

class StatsResponse(BaseModel):
    documents: int
    embedding_model: str
    db_file: str
    embedding_backend: str
    embedding_dimension: Optional[int] = None

# ---------------------------------------------------------------------------
# DB + Embedding helpers
# ---------------------------------------------------------------------------

def connect_sqlite(path: str) -> sqlite3.Connection:
    # check_same_thread=False so we can reuse this connection across FastAPI's threadpool workers.
    # We add a global lock for write operations to keep things simple & thread-safe.
    conn = sqlite3.connect(path, check_same_thread=False)
    conn.row_factory = sqlite3.Row
    conn.enable_load_extension(True)
    try:
        import sqlite_vec  # type: ignore
        sqlite_vec.load(conn)
    except ImportError as exc:
        conn.close()
        raise RuntimeError("sqlite-vec not installed. `pip install sqlite-vec`." ) from exc
    finally:
        conn.enable_load_extension(False)
    return conn


def ensure_schema(conn: sqlite3.Connection) -> None:
    conn.execute(
        """
        CREATE TABLE IF NOT EXISTS docs (
            id INTEGER PRIMARY KEY,
            text TEXT NOT NULL,
            embedding BLOB NOT NULL,
            created_at INTEGER,
            updated_at INTEGER
        )
        """
    )
    # Add columns if previously missing
    info = conn.execute('PRAGMA table_info(docs)').fetchall()
    cols = {r[1] for r in info}
    if 'created_at' not in cols:
        conn.execute('ALTER TABLE docs ADD COLUMN created_at INTEGER')
    if 'updated_at' not in cols:
        conn.execute('ALTER TABLE docs ADD COLUMN updated_at INTEGER')
    now_ts = int(time.time())
    conn.execute('UPDATE docs SET created_at = COALESCE(created_at, ?), updated_at = COALESCE(updated_at, ?)', (now_ts, now_ts))
    conn.commit()


def load_embedding_model() -> Any:
    """Load local sentence-transformers model (only if backend == local)."""
    if EMBEDDING_BACKEND != "local":
        raise RuntimeError("Attempted to load local embedding model while backend is not 'local'")
    global _embedding_model
    if _embedding_model is None:
        if SentenceTransformer is None:
            raise RuntimeError("sentence-transformers not installed")
        _embedding_model = SentenceTransformer(EMBEDDING_MODEL_NAME)
    return _embedding_model  # type: ignore


def gemini_client():  # lazy
    global _gemini_client
    if _gemini_client is not None:
        return _gemini_client
    if genai is None or not GOOGLE_API_KEY:
        return None
    try:
        _gemini_client = genai.Client(api_key=GOOGLE_API_KEY)
    except Exception:
        _gemini_client = None
    return _gemini_client


def _now() -> int:
    return int(time.time())


def _normalize(v: np.ndarray) -> np.ndarray:
    n = float(np.linalg.norm(v))
    if n == 0:
        return v
    return v / n


def embed_texts(texts: Sequence[str], *, is_query: bool = False) -> List[np.ndarray]:
    """Embed a batch of texts using the configured backend.

    Returns list of float32 numpy arrays.
    For Gemini: if dimension != 3072 we L2 normalize (per docs) to approximate cosine similarity using L2 distances.
    """
    if not texts:
        return []
    if EMBEDDING_BACKEND == "local":
        model = load_embedding_model()
        # sentence-transformers already can batch efficiently
        arr = model.encode(list(texts), batch_size=min(len(texts), 32))
        return [np.asarray(v, dtype=np.float32) for v in arr]
    if EMBEDDING_BACKEND == "gemini":
        client = gemini_client()
        if client is None:
            raise RuntimeError("Gemini client not initialized or API key missing for gemini embedding backend")
        if genai_types is None:
            raise RuntimeError("google.genai.types not available")
        task_type = GEMINI_TASK_QUERY if is_query else GEMINI_TASK_DOC
        cfg_kwargs = {"task_type": task_type}
        # Only pass output_dimensionality if not the default 3072 to allow truncation
        if GEMINI_EMBEDDING_DIM and GEMINI_EMBEDDING_DIM != 3072:
            cfg_kwargs["output_dimensionality"] = GEMINI_EMBEDDING_DIM
        try:
            resp = client.models.embed_content(
                model=GEMINI_EMBEDDING_MODEL,
                contents=list(texts),
                config=genai_types.EmbedContentConfig(**cfg_kwargs),
            )
        except Exception as e:  # pragma: no cover
            raise RuntimeError(f"Gemini embed_content failed: {e}")
        out: List[np.ndarray] = []
        for emb_obj in resp.embeddings:
            vec = np.asarray(emb_obj.values, dtype=np.float32)
            # Normalize if truncated dimension (Gemini docs say 3072 already normalized)
            if GEMINI_EMBEDDING_DIM != 3072:
                vec = _normalize(vec).astype(np.float32)
            out.append(vec)
        return out
    raise RuntimeError(f"Unsupported EMBEDDING_BACKEND='{EMBEDDING_BACKEND}'")


def embed(text: str, *, is_query: bool = False) -> np.ndarray:
    return embed_texts([text], is_query=is_query)[0]


# ---------------------------------------------------------------------------
# Query Rewriting (simplified reuse of logic)
# ---------------------------------------------------------------------------
MODE_INSTRUCTIONS = {
    "summarize": "Produce a compressed but information-rich paraphrase suitable for semantic vector search.",
    "expand": "Rewrite the question with explicit entities, context, and missing nouns so it is self-contained.",
    "disambiguate": "Rewrite to remove pronouns and ambiguous references; specify concrete entities.",
    "auto": "Rewrite to be self-contained and explicit for semantic vector search.",
}


def _should_auto_expand(q: str) -> bool:
    short = len(q.strip()) < 20
    pronouns = {"it", "they", "them", "this", "that", "those"}
    tokens = {t.lower().strip(".,!?") for t in q.split()}
    pronouny = any(t in pronouns for t in tokens)
    vague_starters = any(q.lower().startswith(s) for s in ["and then", "then what", "what next", "how about"])
    return short or pronouny or vague_starters


def rewrite_query(original: str, mode: str) -> str:
    client = gemini_client()
    if client is None or mode == "none":
        return original
    applied = mode
    if mode == "auto" and not _should_auto_expand(original):
        return original
    if mode == "auto":
        applied = "expand"
    instruction = MODE_INSTRUCTIONS.get(applied, MODE_INSTRUCTIONS["expand"])
    prompt = (
        "You rewrite user queries for vector similarity search. Output only the rewritten query.\n"
        f"Rewrite style: {instruction}\n"
        f"User: {original}\nRewritten:"
    )
    try:
        resp = client.models.generate_content(model=GEMINI_MODEL_NAME, contents=prompt)
        text = getattr(resp, "text", "").strip().splitlines()[0].strip()
        if text and text not in {"", "Rewritten:"}:
            if (text.startswith('"') and text.endswith('"')) or (text.startswith("'") and text.endswith("'")):
                text = text[1:-1].strip()
            return text or original
        return original
    except Exception:
        return original


# ---------------------------------------------------------------------------
# Core Search
# ---------------------------------------------------------------------------

@dataclass
class Retrieved:
    id: int
    text: str
    distance: float
    created_at: Optional[int]
    updated_at: Optional[int]

def search(query_embedding: np.ndarray, top_k: int, use_recency: bool, half_life: float, alpha: float) -> List[Retrieved]:
    assert _conn is not None
    blob = query_embedding.astype(np.float32).tobytes()
    limit = 50 if use_recency else top_k
    rows = _conn.execute(
        """
        SELECT id, text, vec_distance_l2(embedding, ?) AS distance, created_at, updated_at
        FROM docs
        ORDER BY distance ASC
        LIMIT ?
        """,
        (blob, limit),
    ).fetchall()
    if not use_recency:
        return [Retrieved(int(r["id"]), r["text"], float(r["distance"]), r["created_at"], r["updated_at"]) for r in rows]
    now = _now()
    rescored = []
    for r in rows:
        upd = r["updated_at"] or r["created_at"] or now
        age = max(0, now - int(upd))
        recency_factor = math.exp(-age / half_life)
        adjusted = float(r["distance"]) - alpha * recency_factor
        rescored.append((adjusted, Retrieved(int(r["id"]), r["text"], float(r["distance"]), r["created_at"], r["updated_at"])) )
    rescored.sort(key=lambda x: x[0])
    return [r for _s, r in rescored[:top_k]]


# ---------------------------------------------------------------------------
# API Endpoints
# ---------------------------------------------------------------------------

@app.on_event("startup")
def _startup():  # pragma: no cover
    global _conn
    _conn = connect_sqlite(DB_FILE)
    ensure_schema(_conn)
    # Dimension consistency check: if DB already has embeddings ensure same as current backend
    try:
        row = _conn.execute("SELECT length(embedding) AS l FROM docs LIMIT 1").fetchone()
        existing_dim = None
        if row and row["l"]:
            existing_dim = int(row["l"]) // 4  # float32 bytes -> dimension
        configured_dim = None
        if EMBEDDING_BACKEND == "local":
            # Lazy dimension determination: attempt small dummy encode
            try:
                model = load_embedding_model()
                configured_dim = int(getattr(model, "get_sentence_embedding_dimension", lambda: len(model.encode([""], batch_size=1)[0]))())
            except Exception:
                configured_dim = None
        elif EMBEDDING_BACKEND == "gemini":
            configured_dim = GEMINI_EMBEDDING_DIM
        if existing_dim is not None and configured_dim is not None and existing_dim != configured_dim:
            print(
                f"WARNING: Existing DB embedding dimension {existing_dim} != configured backend dimension {configured_dim}. "
                "You should rebuild the database to avoid inconsistent distances."
            )
    except Exception as e:  # pragma: no cover
        print(f"Startup dimension check skipped due to error: {e}")
    # lazy load embedding model so startup is fast
    print(f"Vector API started using DB: {DB_FILE} with backend={EMBEDDING_BACKEND}")


@app.on_event("shutdown")
def _shutdown():  # pragma: no cover
    global _conn
    if _conn:
        _conn.close()
        _conn = None


@app.get("/stats", response_model=StatsResponse)
def stats():
    assert _conn is not None
    row = _conn.execute("SELECT COUNT(*) AS c FROM docs").fetchone()
    return StatsResponse(
        documents=int(row["c"]),
        embedding_model=EMBEDDING_MODEL_NAME,
        db_file=DB_FILE,
        embedding_backend=EMBEDDING_BACKEND,
        embedding_dimension=(GEMINI_EMBEDDING_DIM if EMBEDDING_BACKEND == "gemini" else None),
    )


@app.post("/documents", response_model=SearchResultDoc, status_code=201)
def create_document(doc: DocCreate):
    assert _conn is not None
    vec = embed(doc.text, is_query=False)
    ts = _now()
    if doc.id is None:
        # determine next id
        row = _conn.execute("SELECT COALESCE(MAX(id),0)+1 FROM docs").fetchone()
        new_id = int(row[0])
    else:
        # ensure not exists
        existing = _conn.execute("SELECT id FROM docs WHERE id=?", (doc.id,)).fetchone()
        if existing:
            raise HTTPException(status_code=409, detail="Document with that ID already exists")
        new_id = doc.id
    with _db_lock:
        _conn.execute(
            "INSERT INTO docs(id,text,embedding,created_at,updated_at) VALUES(?,?,?,?,?)",
            (new_id, doc.text, vec.tobytes(), ts, ts),
        )
        _conn.commit()
    return SearchResultDoc(id=new_id, text=doc.text, distance=0.0, created_at=ts, updated_at=ts)


@app.get("/documents/{doc_id}", response_model=SearchResultDoc)
def get_document(doc_id: int):
    assert _conn is not None
    row = _conn.execute("SELECT id,text,0.0 AS distance, created_at, updated_at FROM docs WHERE id=?", (doc_id,)).fetchone()
    if not row:
        raise HTTPException(status_code=404, detail="Not found")
    return SearchResultDoc(id=row["id"], text=row["text"], distance=0.0, created_at=row["created_at"], updated_at=row["updated_at"])


@app.put("/documents/{doc_id}", response_model=SearchResultDoc)
def update_document(doc_id: int, payload: DocUpdate):
    assert _conn is not None
    row = _conn.execute("SELECT id FROM docs WHERE id=?", (doc_id,)).fetchone()
    if not row:
        raise HTTPException(status_code=404, detail="Not found")
    vec = embed(payload.text, is_query=False)
    ts = _now()
    with _db_lock:
        _conn.execute(
            "UPDATE docs SET text=?, embedding=?, updated_at=? WHERE id=?",
            (payload.text, vec.tobytes(), ts, doc_id),
        )
        _conn.commit()
    return SearchResultDoc(id=doc_id, text=payload.text, distance=0.0, created_at=ts, updated_at=ts)


@app.delete("/documents/{doc_id}", status_code=204)
def delete_document(doc_id: int):
    assert _conn is not None
    with _db_lock:
        cur = _conn.execute("DELETE FROM docs WHERE id=?", (doc_id,))
        if cur.rowcount == 0:
            raise HTTPException(status_code=404, detail="Not found")
        _conn.commit()
    return None


@app.post("/search", response_model=SearchResponse)
def search_endpoint(body: SearchRequest):
    vec = embed(body.query, is_query=True)
    rewritten = rewrite_query(body.query, body.rewrite_mode)
    # If rewriting changed it, embed rewritten for retrieval
    if rewritten != body.query:
        vec = embed(rewritten, is_query=True)
    results = search(vec, body.top_k, body.use_recency, body.recency_half_life, body.recency_alpha)
    return SearchResponse(
        query=body.query,
        rewritten_query=rewritten,
        top_k=body.top_k,
        results=[
            SearchResultDoc(
                id=r.id,
                text=r.text,
                distance=r.distance,
                created_at=r.created_at,
                updated_at=r.updated_at,
            )
            for r in results
        ],
    )


@app.get("/health")
def health():
    return {"status": "ok"}
