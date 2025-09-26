import os
import sqlite3
import argparse
from dataclasses import dataclass
from typing import Iterable, List, Sequence, Tuple, Optional
import math

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


def _load_all_embeddings(conn: sqlite3.Connection) -> List[Tuple[int, str, np.ndarray]]:
    rows = conn.execute("SELECT id, text, embedding FROM docs ORDER BY id").fetchall()
    out: List[Tuple[int, str, np.ndarray]] = []
    for r in rows:
        vec = np.frombuffer(r["embedding"], dtype=np.float32)
        out.append((int(r["id"]), r["text"], vec))
    return out


def reduce_embeddings_to_3d(vectors: List[np.ndarray]) -> np.ndarray:
    """Reduce high-dim vectors to 3D using PCA (fallback: first 3 dims)."""
    if not vectors:
        return np.zeros((0, 3), dtype=np.float32)
    mat = np.vstack(vectors)
    if mat.shape[1] <= 3:
        # Already 3D or fewer; pad if needed
        if mat.shape[1] < 3:
            pad = np.zeros((mat.shape[0], 3 - mat.shape[1]), dtype=mat.dtype)
            mat = np.hstack([mat, pad])
        return mat.astype(np.float32)
    try:
        from sklearn.decomposition import PCA  # type: ignore
        pca = PCA(n_components=3, random_state=42)
        reduced = pca.fit_transform(mat)
        return reduced.astype(np.float32)
    except Exception:
        # Fallback: just take first 3 dimensions
        return mat[:, :3].astype(np.float32)


def visualize_embeddings(
    conn: sqlite3.Connection,
    embedding_model,
    question: str,
    top_k: int,
    outfile: Optional[str] = None,
    animate: bool = False,
) -> None:
    """Create a 3D scatter plot of document embeddings centered on the query.

    The query will be at (0,0,0). Distances are color-coded; top_k retrieved docs
    are highlighted. If animate=True, attempt a simple rotation animation.
    """
    print("\nPreparing visualization ...")
    query_vec = embedding_model.encode(question).astype(np.float32)
    all_items = _load_all_embeddings(conn)
    if not all_items:
        print("No embeddings present to visualize.")
        return

    # Center by subtracting query so it becomes origin.
    centered_vectors = [vec - query_vec for (_id, _text, vec) in all_items]
    coords3d = reduce_embeddings_to_3d(centered_vectors)
    distances = np.linalg.norm(coords3d, axis=1)

    # Identify top_k nearest docs relative to query in original space via distances.
    nearest_indices = np.argsort(distances)[:top_k]
    nearest_set = set(int(i) for i in nearest_indices.tolist())

    try:
        import matplotlib.pyplot as plt  # type: ignore
        from mpl_toolkits.mplot3d import Axes3D  # noqa: F401  # needed for 3D projection
    except ImportError:
        print("matplotlib not installed. Install with `pip install matplotlib` to enable visualization.")
        return

    fig = plt.figure(figsize=(8, 6))
    ax = fig.add_subplot(111, projection="3d")
    xs, ys, zs = coords3d[:, 0], coords3d[:, 1], coords3d[:, 2]
    sc = ax.scatter(xs, ys, zs, c=distances, cmap="viridis", s=40, alpha=0.85)

    # Highlight top_k
    for idx_in_array in nearest_set:
        ax.scatter(
            xs[idx_in_array],
            ys[idx_in_array],
            zs[idx_in_array],
            c="red",
            s=120,
            marker="o",
            edgecolors="black",
            linewidths=1.0,
        )

    # Query at origin (guaranteed after centering)
    ax.scatter([0], [0], [0], c="black", marker="*", s=250, label="Query")

    # Annotate top_k docs with their ID (optional; avoid clutter for large sets)
    for rank, idx_in_array in enumerate(nearest_indices, start=1):
        doc_id, text, _ = all_items[idx_in_array]
        ax.text(
            xs[idx_in_array],
            ys[idx_in_array],
            zs[idx_in_array],
            f"#{rank} (id={doc_id})",
            fontsize=8,
            color="red",
        )

    ax.set_title("Document Embeddings (Query at Origin)")
    ax.set_xlabel("X")
    ax.set_ylabel("Y")
    ax.set_zlabel("Z")
    fig.colorbar(sc, ax=ax, shrink=0.6, label="Distance from Query (in reduced space)")
    ax.legend(loc="upper right")
    plt.tight_layout()

    saved = False
    if outfile:
        base, ext = os.path.splitext(outfile)
        try:
            plt.savefig(outfile, dpi=140)
            saved = True
            print(f"Saved static 3D plot to {outfile}")
        except Exception as exc:  # pragma: no cover
            print(f"Failed to save static plot: {exc}")

    if animate:
        try:
            from matplotlib import animation  # type: ignore

            def _rotate(angle):
                ax.view_init(elev=30, azim=angle)
                return (ax,)

            anim = animation.FuncAnimation(fig, _rotate, frames=range(0, 360, 4), interval=60, blit=False)
            if outfile:
                gif_path = base + ".gif" if base else "embeddings_rotation.gif"
            else:
                gif_path = "embeddings_rotation.gif"
            try:
                anim.save(gif_path, writer="pillow")
                print(f"Saved rotation animation to {gif_path}")
            except Exception as exc:  # pragma: no cover
                print(f"Animation save failed (install pillow or imagemagick for GIF support): {exc}")
        except Exception as exc:
            print(f"Animation unavailable: {exc}")

    if not saved and not animate:
        print("Showing interactive window (close it to continue)...")
        plt.show()



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
    visualize: bool = False,
    visualize_file: Optional[str] = None,
    animate: bool = False,
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
        if visualize:
            visualize_embeddings(
                conn,
                embedding_model,
                q,
                top_k=top_k,
                outfile=visualize_file,
                animate=animate,
            )

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
    parser.add_argument(
        "--visualize",
        "-v",
        action="store_true",
        help="After answering the question, display or save a 3D embedding visualization.",
    )
    parser.add_argument(
        "--viz-file",
        type=str,
        help="Optional path to save the static visualization image (e.g. embeddings.png).",
    )
    parser.add_argument(
        "--animate",
        action="store_true",
        help="Attempt to also create a rotating GIF (requires pillow). Implies --visualize.",
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
        visualize=args.visualize or args.animate,
        visualize_file=args.viz_file,
        animate=args.animate,
    )