import os
import sqlite3
import argparse
from dataclasses import dataclass
from typing import Iterable, List, Sequence, Tuple, Optional

import numpy as np


DB_FILE = "vector_memory_vec.db"
EMBEDDING_MODEL_NAME = os.getenv("EMBEDDING_MODEL_NAME", "all-MiniLM-L6-v2")
GEMINI_MODEL_NAME = os.getenv("GEMINI_MODEL_NAME", "gemini-2.0-flash-001")
# The modern SDK (google-genai) will auto-detect GEMINI_API_KEY or GOOGLE_API_KEY.
# We still read them explicitly so we can give a friendly message if absent.
GOOGLE_API_KEY = os.getenv("GOOGLE_API_KEY") or os.getenv("GEMINI_API_KEY") or os.getenv("GEMINI_API")

DEFAULT_DOCUMENTS: Sequence[str] = (
    "The capital of France is Paris.",
    "Mitochondria are the powerhouse of the cell.",
    "The 2024 Summer Olympics were held in the French capital.",
    "Photosynthesis is the process used by plants to convert light energy into chemical energy.",
    "The Louvre Museum, located in Paris, is the world's largest art museum.",
    "Cellular respiration releases energy from glucose.",
)


@dataclass
class RetrievedDocument:
    doc_id: int
    text: str
    distance: float


def reset_database(db_file: str = DB_FILE) -> None:
    """Remove the on-disk SQLite file so we always start from a clean slate."""
    if os.path.exists(db_file):
        print(f"Removing existing database file: {db_file}")
        os.remove(db_file)


def connect_sqlite(db_file: str = DB_FILE) -> sqlite3.Connection:
    """Create a SQLite connection with sqlite-vec extension enabled."""
    conn = sqlite3.connect(db_file)
    conn.row_factory = sqlite3.Row
    conn.enable_load_extension(True)
    try:
        import sqlite_vec
    except ImportError as exc:  # pragma: no cover - defensive guard
        conn.close()
        raise RuntimeError(
            "sqlite-vec is required. Install it with `pip install sqlite-vec`."
        ) from exc

    sqlite_vec.load(conn)
    conn.enable_load_extension(False)
    return conn


def ensure_schema(conn: sqlite3.Connection) -> None:
    conn.execute(
        """
        CREATE TABLE IF NOT EXISTS docs (
            id INTEGER PRIMARY KEY,
            text TEXT NOT NULL,
            embedding BLOB NOT NULL
        )
        """
    )
    conn.commit()


def load_embedding_model(model_name: str = EMBEDDING_MODEL_NAME):
    try:
        from sentence_transformers import SentenceTransformer
    except ImportError as exc:  # pragma: no cover - defensive guard
        raise RuntimeError(
            "sentence-transformers is required. Install it with `pip install sentence-transformers`."
        ) from exc

    print(f"Loading embedding model '{model_name}'...")
    return SentenceTransformer(model_name)


def embed_documents(embedding_model, documents: Sequence[str]) -> List[np.ndarray]:
    print("Generating embeddings for documents...")
    embeddings = embedding_model.encode(documents)
    return [np.asarray(emb, dtype=np.float32) for emb in embeddings]


def store_documents(
    conn: sqlite3.Connection,
    documents: Sequence[str],
    embeddings: Sequence[np.ndarray],
) -> None:
    print("Storing documents and embeddings in SQLite...")
    conn.execute("DELETE FROM docs")
    for idx, (doc, embedding) in enumerate(zip(documents, embeddings), start=1):
        conn.execute(
            "INSERT INTO docs(id, text, embedding) VALUES (?, ?, ?)",
            (idx, doc, embedding.tobytes()),
        )
    conn.commit()
    print("Database created and indexed successfully!")


def search_similar_documents(
    conn: sqlite3.Connection,
    query_embedding: np.ndarray,
    top_k: int = 3,
) -> List[RetrievedDocument]:
    query_blob = query_embedding.astype(np.float32).tobytes()
    cursor = conn.execute(
        """
        SELECT id, text, vec_distance_l2(embedding, ?) as distance
        FROM docs
        ORDER BY distance ASC
        LIMIT ?
        """,
        (query_blob, top_k),
    )
    rows = cursor.fetchall()
    return [RetrievedDocument(int(row["id"]), row["text"], float(row["distance"])) for row in rows]


def ensure_gemini_model():
    """Create a google-genai Client (new SDK) or return None if unavailable.

    The previous implementation used genai.configure + GenerativeModel.
    The modern SDK exposes a Client object: client = genai.Client(api_key=...).
    We keep the function name for backwards compatibility with the rest of the script.
    """
    try:
        from google import genai
    except ImportError:  # pragma: no cover - defensive guard
        print(
            "Package 'google-genai' not installed. Install it with `pip install google-genai` to enable Gemini responses."
        )
        return None

    if not GOOGLE_API_KEY:
        # Client() will still look for env vars, but we warn the user explicitly.
        print(
            "No GOOGLE_API_KEY / GEMINI_API_KEY environment variable detected. "
            "Skipping Gemini call (will show retrieved context only)."
        )
        return None

    try:
        client = genai.Client(api_key=GOOGLE_API_KEY)
    except Exception as exc:  # pragma: no cover - defensive guard
        print(f"Failed to create Gemini client: {exc}")
        return None

    print(f"Gemini client created (model='{GEMINI_MODEL_NAME}').")
    return client


def build_rag_prompt(question: str, context_docs: Sequence[RetrievedDocument]) -> str:
    if not context_docs:
        return (
            "You are a knowledgeable assistant. The user asked: "
            f"{question}. No additional context is available; answer as best you can."
        )

    context = "\n\n".join(
        f"Document {doc.doc_id} (distance {doc.distance:.4f}):\n{doc.text}" for doc in context_docs
    )

    return (
        "You are a knowledgeable assistant. Use ONLY the context that follows to answer the question.\n"
        "If the context is insufficient, say you don't know.\n\n"
        f"Context:\n{context}\n\n"
        f"Question: {question}\n"
        "Answer in a concise paragraph."
    )


def call_gemini(client, prompt: str) -> str:
    if client is None:
        return "[Gemini not configured – displaying retrieved context only.]"

    try:
        # New SDK signature: client.models.generate_content(model=..., contents=...)
        response = client.models.generate_content(
            model=GEMINI_MODEL_NAME,
            contents=prompt,
        )
    except Exception as exc:  # pragma: no cover - defensive guard
        return f"Failed to generate content with Gemini: {exc}"

    # response.text collects the concatenated text parts.
    return getattr(response, "text", str(response))


def pretty_print_results(question: str, docs: Sequence[RetrievedDocument]) -> None:
    print("\n--- Performing a search ---")
    print(f"Query: '{question}'")
    if not docs:
        print("No similar documents found in the vector store.")
        return

    print("\nTop results from vector memory:")
    for doc in docs:
        print(f"  - (ID: {doc.doc_id}, Distance: {doc.distance:.4f}): '{doc.text}'")


def ensure_index(
    conn: sqlite3.Connection,
    embedding_model,
    documents: Sequence[str],
    rebuild: bool,
) -> None:
    """(Re)build the vector index if requested or if empty."""
    need_rebuild = rebuild
    if not need_rebuild:
        row = conn.execute("SELECT COUNT(*) as c FROM docs").fetchone()
        if row["c"] == 0:
            need_rebuild = True
    if not need_rebuild:
        print("Reusing existing vector index (use --rebuild to force regeneration).")
        return
    print("Building (or rebuilding) vector index...")
    doc_embeddings = embed_documents(embedding_model, documents)
    store_documents(conn, documents, doc_embeddings)


def answer_question(
    question: str,
    conn: sqlite3.Connection,
    embedding_model,
    top_k: int,
) -> str:
    query_embedding = embedding_model.encode(question)
    retrieved_docs = search_similar_documents(conn, query_embedding, top_k=top_k)
    pretty_print_results(question, retrieved_docs)
    prompt = build_rag_prompt(question, retrieved_docs)
    gemini_client = ensure_gemini_model()
    answer = call_gemini(gemini_client, prompt)
    print("\n--- Gemini Response ---")
    print(answer)
    return answer


def interactive_loop(conn: sqlite3.Connection, embedding_model, top_k: int) -> None:
    print("Entering interactive mode. Type 'exit', 'quit', or ':q' to leave.")
    while True:
        try:
            question = input("\nYour question> ").strip()
        except (KeyboardInterrupt, EOFError):  # graceful exit
            print("\nExiting interactive mode.")
            break
        if question.lower() in {"exit", "quit", ":q"}:
            print("Goodbye.")
            break
        if not question:
            continue
        answer_question(question, conn, embedding_model, top_k)


def run_demo(
    documents: Sequence[str] = DEFAULT_DOCUMENTS,
    question: Optional[str] = None,
    top_k: int = 3,
    rebuild: bool = True,
    interactive: bool = False,
) -> None:
    if rebuild:
        reset_database()
    conn = connect_sqlite()
    ensure_schema(conn)
    embedding_model = load_embedding_model()
    ensure_index(conn, embedding_model, documents, rebuild=rebuild)

    if interactive:
        interactive_loop(conn, embedding_model, top_k)
    else:
        # Use default demo question if none supplied.
        q = question or "What is the energy source for a cell?"
        answer_question(q, conn, embedding_model, top_k)

    conn.close()


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Vector Search + Gemini RAG Demo")
    parser.add_argument(
        "--question",
        "-q",
        type=str,
        help="Single question to ask (skips interactive mode unless --interactive provided).",
    )
    parser.add_argument(
        "--top-k",
        type=int,
        default=3,
        help="Number of most similar documents to retrieve (default: 3)",
    )
    parser.add_argument(
        "--no-rebuild",
        action="store_true",
        help="Reuse existing database & vectors if present (do not regenerate embeddings).",
    )
    parser.add_argument(
        "--interactive",
        "-i",
        action="store_true",
        help="Enter an interactive Q&A loop after (re)building index.",
    )
    parser.add_argument(
        "--docs-file",
        type=str,
        help="Optional path to a text file (one document per line) to replace DEFAULT_DOCUMENTS.",
    )
    return parser.parse_args()


def load_documents_from_file(path: str) -> Sequence[str]:
    with open(path, "r", encoding="utf-8") as f:
        docs = [line.strip() for line in f.readlines() if line.strip()]
    if not docs:
        raise ValueError("No non-empty lines found in docs file.")
    print(f"Loaded {len(docs)} documents from {path}.")
    return docs


if __name__ == "__main__":
    args = parse_args()
    if args.docs_file:
        documents = load_documents_from_file(args.docs_file)
    else:
        documents = DEFAULT_DOCUMENTS
    run_demo(
        documents=documents,
        question=args.question,
        top_k=args.top_k,
        rebuild=not args.no_rebuild,
        interactive=args.interactive,
    )