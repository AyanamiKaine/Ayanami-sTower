import os
import argparse
import sqlite3
import time
import hashlib
from dataclasses import dataclass
from typing import List, Sequence, Optional, Iterable, Tuple

import numpy as np

# Reuse the same embedding model environment variable if present
EMBEDDING_MODEL_NAME = os.getenv("EMBEDDING_MODEL_NAME", "all-MiniLM-L6-v2")
DB_DEFAULT = "vault_embeddings.db"

# --------------------------- Data Structures ---------------------------
@dataclass
class IngestResult:
    new: int
    updated: int
    skipped: int
    total_chunks: int
    db_path: str


# --------------------------- DB Helpers ---------------------------

def connect_sqlite(db_path: str) -> sqlite3.Connection:
    conn = sqlite3.connect(db_path)
    conn.row_factory = sqlite3.Row
    conn.enable_load_extension(True)
    try:
        import sqlite_vec  # type: ignore
        sqlite_vec.load(conn)
    except ImportError as exc:
        conn.close()
        raise RuntimeError("sqlite-vec is required. Install with `pip install sqlite-vec`." ) from exc
    finally:
        conn.enable_load_extension(False)
    return conn


def ensure_schema(conn: sqlite3.Connection) -> None:
    conn.execute(
        """
        CREATE TABLE IF NOT EXISTS docs (
            id INTEGER PRIMARY KEY,
            path TEXT NOT NULL,
            chunk_index INTEGER NOT NULL,
            chunk_key TEXT NOT NULL UNIQUE,
            title TEXT,
            content TEXT NOT NULL,
            embedding BLOB NOT NULL,
            content_hash TEXT NOT NULL,
            mtime INTEGER NOT NULL,
            created_at INTEGER NOT NULL,
            updated_at INTEGER NOT NULL
        )
        """
    )
    conn.execute("CREATE INDEX IF NOT EXISTS idx_docs_path ON docs(path)")
    conn.commit()


def drop_all(conn: sqlite3.Connection) -> None:
    conn.execute("DROP TABLE IF EXISTS docs")
    conn.commit()


# --------------------------- Embeddings ---------------------------

def load_embedding_model(name: str = EMBEDDING_MODEL_NAME):
    try:
        from sentence_transformers import SentenceTransformer  # type: ignore
    except ImportError as exc:  # pragma: no cover
        raise RuntimeError("sentence-transformers not installed. `pip install sentence-transformers`." ) from exc
    print(f"Loading embedding model '{name}' ...")
    return SentenceTransformer(name)


def embed_texts(model, texts: Sequence[str], batch_size: int = 16) -> List[np.ndarray]:
    # SentenceTransformer already batches internally, but we allow explicit batch control if needed.
    embs = model.encode(list(texts), batch_size=batch_size)
    return [np.asarray(e, dtype=np.float32) for e in embs]


# --------------------------- Vault Crawling & Chunking ---------------------------

def list_markdown_files(root: str) -> List[str]:
    out: List[str] = []
    for dirpath, _dirs, files in os.walk(root):
        for f in files:
            if f.lower().endswith(".md"):
                full = os.path.join(dirpath, f)
                out.append(os.path.abspath(full))
    return sorted(out)


def read_file(path: str) -> str:
    with open(path, "r", encoding="utf-8", errors="ignore") as fh:
        return fh.read()


def extract_title(md: str, fallback: str) -> str:
    for line in md.splitlines():
        line = line.strip()
        if line.startswith("#"):
            # first heading
            return line.lstrip("# ").strip() or fallback
    return fallback


def chunk_by_headings(md: str) -> List[Tuple[str, str]]:
    """Split markdown by top-level & secondary headings (#, ##) retaining heading as part of chunk.
    Returns list of (section_title, chunk_text).
    """
    lines = md.splitlines()
    chunks: List[Tuple[str, List[str]]] = []
    current_title = "(intro)"
    current_acc: List[str] = []
    for line in lines:
        if line.startswith("#"):
            # flush previous
            if current_acc:
                chunks.append((current_title, current_acc))
            current_title = line.lstrip("# ").strip() or current_title
            current_acc = [line]
        else:
            current_acc.append(line)
    if current_acc:
        chunks.append((current_title, current_acc))
    # Join
    return [(title, "\n".join(acc).strip()) for title, acc in chunks if any(t.strip() for t in acc)]


def chunk_fixed(md: str, chunk_size: int, overlap: int) -> List[Tuple[str, str]]:
    text = md.strip()
    if not text:
        return []
    chunks: List[Tuple[str, str]] = []
    start = 0
    idx = 0
    while start < len(text):
        end = min(len(text), start + chunk_size)
        seg = text[start:end]
        title = f"chunk-{idx}"
        chunks.append((title, seg))
        if end >= len(text):
            break
        start = end - overlap if overlap > 0 else end
        if start < 0:
            start = 0
        idx += 1
    return chunks


def compute_hash(content: str) -> str:
    return hashlib.sha256(content.encode("utf-8", errors="ignore")).hexdigest()


# --------------------------- Ingestion Logic ---------------------------

def upsert_chunks(
    conn: sqlite3.Connection,
    model,
    path: str,
    chunks: List[Tuple[str, str]],
    mtime: float,
    batch: int = 16,
    dry_run: bool = False,
) -> Tuple[int, int, int]:
    """Upsert chunks for a single file.
    Returns (new, updated, skipped)
    """
    if not chunks:
        return (0, 0, 0)

    now_ts = int(time.time())
    # Pre-compute hashes & decide actions
    rows_meta = []  # (chunk_index, chunk_key, title, content, hash, needs_embedding, existing_id)
    for idx, (title, content) in enumerate(chunks):
        content_hash = compute_hash(content)
        chunk_key = f"{path}:::{idx}"
        row = conn.execute(
            "SELECT id, content_hash FROM docs WHERE chunk_key = ?", (chunk_key,)
        ).fetchone()
        if row:
            if row["content_hash"] == content_hash:
                rows_meta.append((idx, chunk_key, title, content, content_hash, False, row["id"]))
            else:
                rows_meta.append((idx, chunk_key, title, content, content_hash, True, row["id"]))
        else:
            rows_meta.append((idx, chunk_key, title, content, content_hash, True, None))

    # Filter to embed
    to_embed_texts = [m[3] for m in rows_meta if m[5]]
    embeddings: List[np.ndarray] = []
    if to_embed_texts and not dry_run:
        embeddings = embed_texts(model, to_embed_texts)
    emb_iter = iter(embeddings)

    new = updated = skipped = 0
    for meta in rows_meta:
        idx, chunk_key, title, content, content_hash, needs_emb, existing_id = meta
        if not needs_emb:
            skipped += 1
            continue
        vector = next(emb_iter)
        mtime_int = int(mtime)
        if existing_id is None:
            # insert
            if not dry_run:
                conn.execute(
                    "INSERT INTO docs(path, chunk_index, chunk_key, title, content, embedding, content_hash, mtime, created_at, updated_at) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                    (
                        path,
                        idx,
                        chunk_key,
                        title,
                        content,
                        vector.tobytes(),
                        content_hash,
                        mtime_int,
                        now_ts,
                        now_ts,
                    ),
                )
            new += 1
        else:
            if not dry_run:
                conn.execute(
                    "UPDATE docs SET title = ?, content = ?, embedding = ?, content_hash = ?, mtime = ?, updated_at = ? WHERE id = ?",
                    (
                        title,
                        content,
                        vector.tobytes(),
                        content_hash,
                        mtime_int,
                        now_ts,
                        existing_id,
                    ),
                )
            updated += 1
    if not dry_run:
        conn.commit()
    return new, updated, skipped


def ingest_vault(
    root: str,
    db_path: str,
    model_name: str,
    mode: str = "heading",
    chunk_size: int = 1800,
    overlap: int = 200,
    batch_size: int = 16,
    dry_run: bool = False,
) -> IngestResult:
    files = list_markdown_files(root)
    if not files:
        raise SystemExit(f"No markdown files found under {root}")
    print(f"Discovered {len(files)} markdown file(s).")

    conn = connect_sqlite(db_path)
    ensure_schema(conn)
    model = None if dry_run else load_embedding_model(model_name)

    total_new = total_updated = total_skipped = total_chunks = 0
    for fpath in files:
        try:
            content = read_file(fpath)
        except Exception as exc:
            print(f"[WARN] Failed to read {fpath}: {exc}")
            continue
        title = extract_title(content, os.path.basename(fpath))
        mtime = os.path.getmtime(fpath)
        if mode == "heading":
            raw_chunks = chunk_by_headings(content)
        else:
            raw_chunks = chunk_fixed(content, chunk_size=chunk_size, overlap=overlap)
        # Ensure at least one chunk
        if not raw_chunks:
            raw_chunks = [(title, content)]

        # Prepend title to each chunk (context) but avoid duplication if already included
        normalized_chunks: List[Tuple[str, str]] = []
        for idx, (sect_title, chunk_text) in enumerate(raw_chunks):
            if sect_title not in chunk_text.splitlines()[0:2]:
                combined = f"# {sect_title}\n\n{chunk_text}" if not chunk_text.startswith("#") else chunk_text
            else:
                combined = chunk_text
            normalized_chunks.append((sect_title, combined))

        new, updated, skipped = upsert_chunks(
            conn,
            model,
            fpath,
            normalized_chunks,
            mtime,
            batch=batch_size,
            dry_run=dry_run,
        )
        chunk_count = len(normalized_chunks)
        total_new += new
        total_updated += updated
        total_skipped += skipped
        total_chunks += chunk_count
        print(
            f"[{'DRY' if dry_run else 'INGEST'}] {os.path.relpath(fpath, root)} -> chunks={chunk_count} new={new} updated={updated} skipped={skipped}"
        )

    print(
        f"Summary: files={len(files)} chunks={total_chunks} new={total_new} updated={total_updated} skipped={total_skipped}"
    )
    if not dry_run:
        conn.close()
    return IngestResult(total_new, total_updated, total_skipped, total_chunks, db_path)


# --------------------------- CLI ---------------------------

def parse_args() -> argparse.Namespace:
    p = argparse.ArgumentParser(
        description="Ingest a markdown knowledge vault recursively into a sqlite-vec embedding DB (RAG ready)."
    )
    p.add_argument("root", help="Root folder of markdown vault (recursively processed).")
    p.add_argument("--db", default=DB_DEFAULT, help=f"SQLite database file (default: {DB_DEFAULT})")
    p.add_argument("--model", default=EMBEDDING_MODEL_NAME, help="Embedding model name (sentence-transformers).")
    p.add_argument(
        "--mode",
        choices=["heading", "fixed"],
        default="heading",
        help="Chunking strategy: heading = split on headings; fixed = fixed-size windows.",
    )
    p.add_argument("--chunk-size", type=int, default=1800, help="Fixed mode: chunk size in characters (default 1800).")
    p.add_argument("--overlap", type=int, default=200, help="Fixed mode: overlap between chunks (default 200).")
    p.add_argument("--batch-size", type=int, default=16, help="Embedding batch size hint (default 16).")
    p.add_argument("--rebuild", action="store_true", help="Drop existing table and rebuild schema before ingest.")
    p.add_argument("--dry-run", action="store_true", help="Traverse & plan but do not embed or write DB.")
    return p.parse_args()


def main():
    args = parse_args()
    if not os.path.isdir(args.root):
        raise SystemExit(f"Root path '{args.root}' is not a directory")
    conn = connect_sqlite(args.db)
    if args.rebuild and not args.dry_run:
        print("Dropping existing schema ...")
        drop_all(conn)
    ensure_schema(conn)
    conn.close()

    ingest_vault(
        root=args.root,
        db_path=args.db,
        model_name=args.model,
        mode=args.mode,
        chunk_size=args.chunk_size,
        overlap=args.overlap,
        batch_size=args.batch_size,
        dry_run=args.dry_run,
    )


if __name__ == "__main__":
    main()
