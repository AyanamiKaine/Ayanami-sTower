import os
import sqlite3
import argparse
from dataclasses import dataclass
from typing import Iterable, List, Sequence, Tuple, Optional
import math
import json
import time
from datetime import datetime, timezone

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
    "Embeddings are numerical representations that capture the relationships between different inputs. Text embeddings achieve this by converting text into arrays of floating-point numbers known as vectors. The primary purpose of these vectors is to encapsulate the semantic meaning of the text. The dimensionality of the vector, which is the length of the embedding array, can be quite large, with a passage of text sometimes being represented by a vector with hundreds of dimensions.",
    'Using the LLM to mutate the user query is the way to go. A common practice for example to take the chat history of a chat, and rephrase a follow up question that might not have a lot of information density (e.g. follow up question is "and then what?" which is useless for search, but the LLM turns it into "after a contract cancellation, what steps have to be taken afterwards" or something similar, which provides a lot more meat to search with. Using the LLM to mutate the input so it can be used better for search is a path that works very well (ignoring added latency and cost).',
    "Ask the LLM to summarize the question, then take an embedding of that. I think you can do the same with data you store… summarize it to same number of tokens, then get an embedding for that to save with the original text.",
    "This really captures something I've been experiencing with Gemini lately. The models are genuinely capable when they work properly, but there's this persistent truncation issue that makes them unreliable in practice.",
    '''
    Things You Should Never Do, Part I By Joel Spolsky
    Top 10, CEO, News
    Netscape 6.0 is finally going into its first public beta. There never was a version 5.0. The last major release, version 4.0, was released almost three years ago. Three years is an awfully long time in the Internet world. During this time, Netscape sat by, helplessly, as their market share plummeted.

    It’s a bit smarmy of me to criticize them for waiting so long between releases. They didn’t do it on purpose, now, did they?

    Well, yes. They did. They did it by making the single worst strategic mistake that any software company can make:

    They decided to rewrite the code from scratch.

    Netscape wasn’t the first company to make this mistake. Borland made the same mistake when they bought Arago and tried to make it into dBase for Windows, a doomed project that took so long that Microsoft Access ate their lunch, then they made it again in rewriting Quattro Pro from scratch and astonishing people with how few features it had. Microsoft almost made the same mistake, trying to rewrite Word for Windows from scratch in a doomed project called Pyramid which was shut down, thrown away, and swept under the rug. Lucky for Microsoft, they had never stopped working on the old code base, so they had something to ship, making it merely a financial disaster, not a strategic one.

    We’re programmers. Programmers are, in their hearts, architects, and the first thing they want to do when they get to a site is to bulldoze the place flat and build something grand. We’re not excited by incremental renovation: tinkering, improving, planting flower beds.

    There’s a subtle reason that programmers always want to throw away the code and start over. The reason is that they think the old code is a mess. And here is the interesting observation: they are probably wrong. The reason that they think the old code is a mess is because of a cardinal, fundamental law of programming:

    It’s harder to read code than to write it.

    This is why code reuse is so hard. This is why everybody on your team has a different function they like to use for splitting strings into arrays of strings. They write their own function because it’s easier and more fun than figuring out how the old function works.

    As a corollary of this axiom, you can ask almost any programmer today about the code they are working on. “It’s a big hairy mess,” they will tell you. “I’d like nothing better than to throw it out and start over.”

    Why is it a mess?

    “Well,” they say, “look at this function. It is two pages long! None of this stuff belongs in there! I don’t know what half of these API calls are for.”

    Before Borland’s new spreadsheet for Windows shipped, Philippe Kahn, the colorful founder of Borland, was quoted a lot in the press bragging about how Quattro Pro would be much better than Microsoft Excel, because it was written from scratch. All new source code! As if source code rusted.

    The idea that new code is better than old is patently absurd. Old code has been used. It has been tested. Lots of bugs have been found, and they’ve been fixed. There’s nothing wrong with it. It doesn’t acquire bugs just by sitting around on your hard drive. Au contraire, baby! Is software supposed to be like an old Dodge Dart, that rusts just sitting in the garage? Is software like a teddy bear that’s kind of gross if it’s not made out of all new material?

    Back to that two page function. Yes, I know, it’s just a simple function to display a window, but it has grown little hairs and stuff on it and nobody knows why. Well, I’ll tell you why: those are bug fixes. One of them fixes that bug that Nancy had when she tried to install the thing on a computer that didn’t have Internet Explorer. Another one fixes that bug that occurs in low memory conditions. Another one fixes that bug that occurred when the file is on a floppy disk and the user yanks out the disk in the middle. That LoadLibrary call is ugly but it makes the code work on old versions of Windows 95.

    Each of these bugs took weeks of real-world usage before they were found. The programmer might have spent a couple of days reproducing the bug in the lab and fixing it. If it’s like a lot of bugs, the fix might be one line of code, or it might even be a couple of characters, but a lot of work and time went into those two characters.

    When you throw away code and start from scratch, you are throwing away all that knowledge. All those collected bug fixes. Years of programming work.

    You are throwing away your market leadership. You are giving a gift of two or three years to your competitors, and believe me, that is a long time in software years.

    You are putting yourself in an extremely dangerous position where you will be shipping an old version of the code for several years, completely unable to make any strategic changes or react to new features that the market demands, because you don’t have shippable code. You might as well just close for business for the duration.

    You are wasting an outlandish amount of money writing code that already exists.



    Is there an alternative? The consensus seems to be that the old Netscape code base was really bad. Well, it might have been bad, but, you know what? It worked pretty darn well on an awful lot of real world computer systems.

    When programmers say that their code is a holy mess (as they always do), there are three kinds of things that are wrong with it.

    First, there are architectural problems. The code is not factored correctly. The networking code is popping up its own dialog boxes from the middle of nowhere; this should have been handled in the UI code. These problems can be solved, one at a time, by carefully moving code, refactoring, changing interfaces. They can be done by one programmer working carefully and checking in his changes all at once, so that nobody else is disrupted. Even fairly major architectural changes can be done without throwing away the code. On the Juno project we spent several months rearchitecting at one point: just moving things around, cleaning them up, creating base classes that made sense, and creating sharp interfaces between the modules. But we did it carefully, with our existing code base, and we didn’t introduce new bugs or throw away working code.

    A second reason programmers think that their code is a mess is that it is inefficient. The rendering code in Netscape was rumored to be slow. But this only affects a small part of the project, which you can optimize or even rewrite. You don’t have to rewrite the whole thing. When optimizing for speed, 1% of the work gets you 99% of the bang.

    Third, the code may be doggone ugly. One project I worked on actually had a data type called a FuckedString. Another project had started out using the convention of starting member variables with an underscore, but later switched to the more standard “m_”. So half the functions started with “_” and half with “m_”, which looked ugly. Frankly, this is the kind of thing you solve in five minutes with a macro in Emacs, not by starting from scratch.

    It’s important to remember that when you start from scratch there is absolutely no reason to believe that you are going to do a better job than you did the first time. First of all, you probably don’t even have the same programming team that worked on version one, so you don’t actually have “more experience”. You’re just going to make most of the old mistakes again, and introduce some new problems that weren’t in the original version.

    The old mantra build one to throw away is dangerous when applied to large scale commercial applications. If you are writing code experimentally, you may want to rip up the function you wrote last week when you think of a better algorithm. That’s fine. You may want to refactor a class to make it easier to use. That’s fine, too. But throwing away the whole program is a dangerous folly, and if Netscape actually had some adult supervision with software industry experience, they might not have shot themselves in the foot so badly.

    ''',
    '''
    Avram Joel Spolsky (Hebrew: אברם יואל ספולסקי; born 1965) is a software engineer and writer. He is the author of Joel on Software, a blog on software development, and the creator of the project management software Trello.[2] He was a Program Manager on the Microsoft Excel team between 1991 and 1994. He later founded Fog Creek Software in 2000 and launched the Joel on Software blog. In 2008, he launched the Stack Overflow programmer Q&A site in collaboration with Jeff Atwood. Using the Stack Exchange software product which powers Stack Overflow, the Stack Exchange Network now hosts over 170 Q&A sites.''',
    "In the beginning, I was really curious about ChatGPT—a tool that could save me from useless blogs, pushy products, and research roadblocks. Then it started asking follow-up questions, and I got a bit uneasy… where is this trying to take me? Now it feels like the goal is to pull me into a discussion, ultimately consulting me on what? Buy something? Think something? It’s sad to see something so promising turn into an annoying, social-network-like experience, just like so many technologies before. As with Facebook or Google products, maybe we’re not the happy users of free tech—we’re the product. Or am I completely off here? For me, there’s a clear boundary between me controlling a tool and a tool broadcasting at me. Problems with LLM",
    "Don’t forget that Sam Altman is also the cryptocurrency scammer who wants your biometric information. The goal was and will always be personal wealth and power, not helping others.",
    '''Pulse introduces this future in its simplest form: personalized research and timely updates that appear regularly to keep you informed. Soon, Pulse will be able to connect with more of the apps you use so updates capture a more complete picture of your context. We’re also exploring ways for Pulse to deliver relevant work at the right moments throughout the day, whether it’s a quick check before a meeting, a reminder to revisit a draft, or a resource that appears right when you need it.
This reads to me like OAI is seeking to build an advertising channel into their product stack.''',
'''Ollama is a business? They raised money? I thought it was just a useful open source product.
I wonder how they plan to monetize their users. Doesn't sound promising.''',
'''Nit: It's the Pi 500+ (the + was eaten up by HN's automated title sensationalism-removal, I guess)
And I've posted benchmark data to my sbc-reviews repo here: https://github.com/geerlingguy/sbc-reviews/issues/81

Performance-wise it's pretty much the same as the Pi 5 16GB (and can be slightly faster than the regular Pi 500 depending on the task, if it benefits from faster storage or more RAM...) Since this is the first Pi with built-in NVMe'''
)


@dataclass
class RetrievedDocument:
    doc_id: int
    text: str
    distance: float
    created_at: Optional[int] = None
    updated_at: Optional[int] = None


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
    """Ensure base table exists and timestamp columns (created_at, updated_at) are present.

    We lazily ALTER TABLE to add missing columns so existing deployments upgrade in-place.
    created_at / updated_at are stored as INTEGER epoch seconds (UTC).
    """
    conn.execute(
        """
        CREATE TABLE IF NOT EXISTS docs (
            id INTEGER PRIMARY KEY,
            text TEXT NOT NULL,
            embedding BLOB NOT NULL
        )
        """
    )
    # Detect existing columns
    info = conn.execute('PRAGMA table_info(docs)').fetchall()
    existing_cols = {row[1] for row in info}
    if 'created_at' not in existing_cols:
        conn.execute('ALTER TABLE docs ADD COLUMN created_at INTEGER')
    if 'updated_at' not in existing_cols:
        conn.execute('ALTER TABLE docs ADD COLUMN updated_at INTEGER')
    # Backfill any NULL timestamps for legacy rows
    now_ts = int(time.time())
    conn.execute('UPDATE docs SET created_at = COALESCE(created_at, ?), updated_at = COALESCE(updated_at, ?)', (now_ts, now_ts))
    conn.commit()


def _now_epoch() -> int:
    return int(time.time())


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
    print("Storing documents and embeddings in SQLite (replacing existing)...")
    conn.execute("DELETE FROM docs")
    ts = _now_epoch()
    for idx, (doc, embedding) in enumerate(zip(documents, embeddings), start=1):
        conn.execute(
            "INSERT INTO docs(id, text, embedding, created_at, updated_at) VALUES (?, ?, ?, ?, ?)",
            (idx, doc, embedding.tobytes(), ts, ts),
        )
    conn.commit()
    print("Database created and indexed successfully!")


def add_text_documents(
    conn: sqlite3.Connection,
    embedding_model,
    new_texts: Sequence[str],
) -> List[int]:
    """Append new raw text documents to the existing vector table without deleting prior data.

    Returns list of inserted row IDs.
    """
    if not new_texts:
        return []
    # Determine next starting id
    row = conn.execute("SELECT COALESCE(MAX(id), 0) FROM docs").fetchone()
    start_id = int(row[0]) if row else 0
    embeddings = embedding_model.encode(new_texts)
    inserted_ids: List[int] = []
    ts = _now_epoch()
    for offset, (text, emb) in enumerate(zip(new_texts, embeddings)):
        emb32 = np.asarray(emb, dtype=np.float32)
        doc_id = start_id + offset + 1
        conn.execute(
            "INSERT INTO docs(id, text, embedding, created_at, updated_at) VALUES (?, ?, ?, ?, ?)",
            (doc_id, text, emb32.tobytes(), ts, ts),
        )
        inserted_ids.append(doc_id)
    conn.commit()
    print(f"Added {len(inserted_ids)} new document(s) to vector store (ids: {inserted_ids}).")
    return inserted_ids


def update_document(
    conn: sqlite3.Connection,
    embedding_model,
    doc_id: int,
    new_text: str,
) -> bool:
    """Update text & embedding for an existing doc; refresh updated_at timestamp.

    Returns True if row existed and was updated.
    """
    row = conn.execute("SELECT id FROM docs WHERE id = ?", (doc_id,)).fetchone()
    if not row:
        return False
    emb = embedding_model.encode([new_text])[0]
    emb32 = np.asarray(emb, dtype=np.float32)
    ts = _now_epoch()
    conn.execute(
        "UPDATE docs SET text = ?, embedding = ?, updated_at = ? WHERE id = ?",
        (new_text, emb32.tobytes(), ts, doc_id),
    )
    conn.commit()
    print(f"Updated document id={doc_id} (updated_at={ts}).")
    return True


def search_similar_documents(
    conn: sqlite3.Connection,
    query_embedding: np.ndarray,
    top_k: int = 3,
    candidate_pool: int = 50,
    recency: bool = False,
    recency_half_life: float = 7 * 24 * 3600.0,
    recency_alpha: float = 0.3,
) -> List[RetrievedDocument]:
    """Retrieve similar docs; optionally apply recency-aware re-ranking.

    When recency=True, we fetch a wider candidate_pool (default 50) by pure vector distance,
    then blend in a recency score based on updated_at (falling back to created_at -> now).

    Recency scoring: recency_factor = exp(-age_seconds / half_life). New docs => factor≈1.
    Adjusted score for ranking = distance - recency_alpha * recency_factor.
    (We subtract so recent docs appear slightly closer.)
    """
    query_blob = query_embedding.astype(np.float32).tobytes()
    limit = candidate_pool if recency else top_k
    cursor = conn.execute(
        """
        SELECT id, text, vec_distance_l2(embedding, ?) as distance, created_at, updated_at
        FROM docs
        ORDER BY distance ASC
        LIMIT ?
        """,
        (query_blob, limit),
    )
    rows = cursor.fetchall()
    if not recency:
        return [
            RetrievedDocument(
                int(r["id"]),
                r["text"],
                float(r["distance"]),
                created_at=r["created_at"],
                updated_at=r["updated_at"],
            )
            for r in rows
        ]
    now = _now_epoch()
    rescored = []
    for r in rows:
        upd = r["updated_at"] if r["updated_at"] is not None else r["created_at"]
        if upd is None:
            upd = now  # treat legacy unset rows as brand new
        age = max(0, now - int(upd))
        recency_factor = math.exp(-age / recency_half_life)
        base_distance = float(r["distance"])
        adjusted = base_distance - recency_alpha * recency_factor
        rescored.append((
            adjusted,
            RetrievedDocument(
                int(r["id"]),
                r["text"],
                base_distance,
                created_at=r["created_at"],
                updated_at=r["updated_at"],
            ),
        ))
    rescored.sort(key=lambda x: x[0])
    return [rd for _score, rd in rescored[:top_k]]


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


def build_rag_prompt(
    original_question: str,
    context_docs: Sequence[RetrievedDocument],
    search_query: Optional[str] = None,
) -> str:
    """Construct the RAG prompt including timestamps.

    Each passage lists distance and UTC created/updated timestamps (if available)
    so the model can prefer fresher information if it chooses.
    """
    if not context_docs:
        return (
            "You are a knowledgeable assistant. The user asked: "
            f"{original_question}. No additional context is available; answer as best you can."
        )

    def _fmt(ts: Optional[int]) -> str:
        if ts is None:
            return "unknown"
        try:
            return datetime.fromtimestamp(int(ts), tz=timezone.utc).strftime("%Y-%m-%d %H:%M:%SZ")
        except Exception:
            return str(ts)

    lines: List[str] = []
    for doc in context_docs:
        ct = _fmt(doc.created_at)
        ut = _fmt(doc.updated_at)
        if doc.updated_at and doc.updated_at != doc.created_at:
            ts_part = f"created {ct}; updated {ut}"
        else:
            ts_part = f"created {ct}"
        lines.append(f"Document {doc.doc_id} (distance {doc.distance:.4f}; {ts_part}):\n{doc.text}")
    context_block = "\n\n".join(lines)

    if search_query and search_query.strip() and search_query.strip() != original_question.strip():
        search_note = (
            "The following 'Search Expansion' is a clarified / rewritten variant of the user's question "
            "that was used to retrieve context passages: '\n" + search_query + "'\n\n"
        )
    else:
        search_note = ""

    return (
        "You are a knowledgeable assistant. Use ONLY the context that follows to answer the ORIGINAL question.\n"
        "If the context is insufficient, say you don't know.\n\n"
        f"Original Question: {original_question}\n"
        f"{search_note}"
        f"Context Passages (each with distance & timestamps):\n{context_block}\n\n"
        "Provide a concise, factual answer. If multiple documents disagree, note the discrepancy."
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
    def _fmt(ts: Optional[int]) -> str:
        if ts is None:
            return "?"
        try:
            return datetime.fromtimestamp(int(ts), tz=timezone.utc).strftime("%Y-%m-%d")
        except Exception:
            return str(ts)
    print("\nTop results from vector memory (timestamps UTC):")
    for doc in docs:
        if doc.updated_at and doc.updated_at != doc.created_at:
            ts_info = f"c:{_fmt(doc.created_at)} u:{_fmt(doc.updated_at)}"
        else:
            ts_info = f"c:{_fmt(doc.created_at)}"
        print(f"  - (ID: {doc.doc_id}, Dist: {doc.distance:.4f}, {ts_info}): '{doc.text}'")


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


def _should_auto_expand(q: str) -> bool:
    short = len(q.strip()) < 20
    pronouns = {"it", "they", "them", "this", "that", "those"}
    tokens = {t.lower().strip(".,!?") for t in q.split()}
    pronouny = any(t in pronouns for t in tokens)
    vague_starters = any(q.lower().startswith(s) for s in ["and then", "then what", "what next", "how about"])
    return short or pronouny or vague_starters


def rewrite_query(
    original: str,
    history: Sequence[dict],
    mode: str,
    gemini_client,
) -> str:
    if gemini_client is None or mode == "none":
        return original
    applied_mode = mode
    if mode == "auto" and not _should_auto_expand(original):
        return original
    if mode == "auto":
        applied_mode = "expand"

    mode_instructions = {
        "summarize": "Produce a compressed but information-rich paraphrase suitable for semantic vector search.",
        "expand": "Rewrite the question with explicit entities, context, and missing nouns so it is fully self-contained for semantic vector search.",
        "disambiguate": "Rewrite the question to remove pronouns and ambiguous references; specify concrete entities inferred from the conversation.",
        "auto": "Rewrite the question to be self-contained and explicit for semantic vector search.",
    }
    instruction = mode_instructions.get(applied_mode, mode_instructions["expand"])  # default to expand

    # Use a trimmed history window; each history item: {'question':..., 'answer':..., 'rewritten':...}
    history_lines: List[str] = []
    for turn in history[-5:]:
        q = turn.get("question")
        rw = turn.get("rewritten") or q
        history_lines.append(f"Q: {q}\nR: {rw}")
    history_block = "\n".join(history_lines) if history_lines else "(no prior turns)"

    prompt = (
        "You are a system that rewrites user questions before vector similarity search.\n"
        "Rewrite ONLY the user question; output a single line with no explanations.\n"
        f"Rewrite style: {instruction}\n"
        "Do not hallucinate facts not implied.\n"
        "Conversation snippet (recent turns):\n" + history_block + "\n\n"
        f"User question to rewrite: {original}\n"
        "Rewritten:"
    )
    try:
        response = gemini_client.models.generate_content(model=GEMINI_MODEL_NAME, contents=prompt)
        text = getattr(response, "text", "").strip()
        # Keep to one line
        first_line = text.splitlines()[0].strip()
        # Strip wrapping quotes if any
        if (first_line.startswith("\"") and first_line.endswith("\"")) or (
            first_line.startswith("'") and first_line.endswith("'")
        ):
            first_line = first_line[1:-1].strip()
        return first_line if first_line else original
    except Exception:
        return original


def answer_question(
    question: str,
    conn: sqlite3.Connection,
    embedding_model,
    top_k: int,
    rewrite_mode: str = "none",
    show_rewrite: bool = False,
    history: Optional[List[dict]] = None,
    use_recency: bool = False,
    recency_half_life: float = 7 * 24 * 3600.0,
    recency_alpha: float = 0.3,
) -> Tuple[str, str]:
    gemini_client = ensure_gemini_model()  # one client for both rewrite and answer phases
    history = history or []
    rewritten = rewrite_query(question, history, rewrite_mode, gemini_client)
    if show_rewrite and rewritten != question:
        print(f"[Rewritten for search] {rewritten}")

    # Embed using rewritten (if changed) for better recall
    query_embedding = embedding_model.encode(rewritten)
    retrieved_docs = search_similar_documents(
        conn,
        query_embedding,
        top_k=top_k,
        recency=use_recency,
        recency_half_life=recency_half_life,
        recency_alpha=recency_alpha,
    )
    pretty_print_results(rewritten, retrieved_docs)

    prompt = build_rag_prompt(original_question=question, context_docs=retrieved_docs, search_query=rewritten)
    answer = call_gemini(gemini_client, prompt)
    print("\n--- Gemini Response ---")
    print(answer)
    return answer, rewritten


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
    label_mode: str = "none",  # none|id|short|full
    export_coords: Optional[str] = None,
    plotly_html: Optional[str] = None,
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
    # Compute original embedding-space L2 distances (full dimensional space)
    diffs = [vec - query_vec for (_id, _text, vec) in all_items]
    original_distances = np.array([float(np.linalg.norm(d)) for d in diffs], dtype=np.float32)

    # Re-run similarity search via DB to get authoritative top_k (sqlite-vec distance)
    # This ensures ranking matches the earlier textual output.
    query_blob = query_vec.astype(np.float32)
    retrieved_again = search_similar_documents(conn, query_blob, top_k=top_k)
    rank_by_id = {doc.doc_id: r + 1 for r, doc in enumerate(retrieved_again)}
    retrieved_id_set = set(rank_by_id.keys())

    # Reduce to 3D (PCA) AFTER computing original distances.
    coords3d = reduce_embeddings_to_3d(diffs)
    reduced_distances = np.linalg.norm(coords3d, axis=1)

    # For convenience keep a list of indices corresponding to retrieved docs in ranking order.
    nearest_indices = [next(i for i,(doc_id, _text, _vec) in enumerate(all_items) if doc_id==doc.doc_id) for doc in retrieved_again]
    nearest_set = set(nearest_indices)

    # Prepare optional coordinate export (before any dimensionality changes)
    if export_coords:
        export_payload = []
        for idx, ((doc_id, text, _vec), coord) in enumerate(zip(all_items, coords3d)):
            export_payload.append({
                "doc_id": doc_id,
                "text": text,
                "x": float(coord[0]),
                "y": float(coord[1]),
                "z": float(coord[2]),
                "original_distance": float(original_distances[idx]),
                "reduced_distance": float(reduced_distances[idx]),
                "retrieved_rank": rank_by_id.get(doc_id),
            })
        try:
            with open(export_coords, "w", encoding="utf-8") as f:
                json.dump(
                    {
                        "question": question,
                        "top_k": top_k,
                        "query_at_origin": True,
                        "distance_definition": {
                            "original_distance": "L2 distance in original embedding space (vector - query)",
                            "reduced_distance": "Euclidean distance after PCA reduction to 3D (not distance preserving)",
                        },
                        "items": export_payload,
                    },
                    f,
                    indent=2,
                    ensure_ascii=False,
                )
            print(f"Exported coordinates JSON to {export_coords}")
        except Exception as exc:
            print(f"Failed to export coords JSON: {exc}")

    # Optional Plotly interactive HTML
    if plotly_html:
        try:
            import plotly.graph_objects as go  # type: ignore
        except ImportError:
            print("Plotly not installed. Install with `pip install plotly` for interactive HTML.")
        else:
            hover_texts = []
            for idx, (doc_id, text, _vec) in enumerate(all_items):
                snippet = text if label_mode == "full" else (text[:80] + ("…" if len(text) > 80 else ""))
                hover_texts.append(
                    "ID {id}<br>{snip}<br>orig={od:.4f}<br>red={rd:.4f}{rank}".format(
                        id=doc_id,
                        snip=snippet,
                        od=original_distances[idx],
                        rd=reduced_distances[idx],
                        rank=f"<br>rank=#{rank_by_id[doc_id]}" if doc_id in rank_by_id else "",
                    )
                )

            colors = ["red" if i in nearest_set else "#1f77b4" for i in range(len(all_items))]
            sizes = [14 if i in nearest_set else 7 for i in range(len(all_items))]

            fig_html = go.Figure(
                data=[
                    go.Scatter3d(
                        x=coords3d[:, 0],
                        y=coords3d[:, 1],
                        z=coords3d[:, 2],
                        mode="markers",
                        marker=dict(size=sizes, color=colors, opacity=0.85),
                        text=hover_texts,
                        hoverinfo="text",
                        name="Docs",
                    ),
                    go.Scatter3d(
                        x=[0],
                        y=[0],
                        z=[0],
                        mode="markers",
                        # Plotly scatter3d supports only a limited set of marker symbols;
                        # 'star' is invalid. Use a larger black diamond-open to distinguish the query.
                        marker=dict(size=14, color="black", symbol="diamond-open"),
                        name="Query",
                        text=["Query (origin)" + "\n" + question],
                        hoverinfo="text",
                    ),
                ]
            )
            fig_html.update_layout(
                title="Document Embeddings (interactive)",
                scene=dict(xaxis_title="X", yaxis_title="Y", zaxis_title="Z"),
            )
            try:
                fig_html.write_html(plotly_html, include_plotlyjs="cdn")
                print(f"Saved interactive Plotly HTML to {plotly_html}")
            except Exception as exc:
                print(f"Failed to save Plotly HTML: {exc}")

    # Matplotlib static plot
    try:
        import matplotlib.pyplot as plt  # type: ignore
        from mpl_toolkits.mplot3d import Axes3D  # noqa: F401
    except ImportError:
        print("matplotlib not installed. Install with `pip install matplotlib` to enable static visualization.")
        return

    fig = plt.figure(figsize=(8, 6))
    ax = fig.add_subplot(111, projection="3d")
    xs, ys, zs = coords3d[:, 0], coords3d[:, 1], coords3d[:, 2]
    sc = ax.scatter(
        xs,
        ys,
        zs,
        c=original_distances,  # color based on original embedding distance for interpretability
        cmap="viridis",
        s=40,
        alpha=0.85,
    )

    # Highlight top_k
    for idx_in_array in nearest_set:
        ax.scatter(
            xs[idx_in_array], ys[idx_in_array], zs[idx_in_array], c="red", s=120, marker="o", edgecolors="black", linewidths=1.0
        )

    # Query at origin
    ax.scatter([0], [0], [0], c="black", marker="*", s=250, label="Query")

    # Label strategy
    def _snippet(text: str, limit: int = 32) -> str:
        return text if len(text) <= limit else text[:limit - 1] + "…"

    max_points_for_auto = 40
    do_label = label_mode != "none" and (len(all_items) <= max_points_for_auto or label_mode in {"id", "short"})
    if do_label:
        for idx, (doc_id, text, _vec) in enumerate(all_items):
            if label_mode == "id":
                lab = str(doc_id)
            elif label_mode == "short":
                lab = f"{doc_id}:" + _snippet(text, 28)
            else:  # full
                lab = f"{doc_id}: {text}"
            ax.text(xs[idx], ys[idx], zs[idx], lab, fontsize=7 if label_mode != "full" else 8, color="dimgray")

    # Always annotate the ranked top_k clearly
    for rank_pos, idx_in_array in enumerate(nearest_indices, start=1):
        doc_id, text, _ = all_items[idx_in_array]
        ax.text(xs[idx_in_array], ys[idx_in_array], zs[idx_in_array], f"#{rank_pos} (id={doc_id})", fontsize=9, color="red")

    ax.set_title("Document Embeddings (Query at Origin)")
    ax.set_xlabel("X")
    ax.set_ylabel("Y")
    ax.set_zlabel("Z")
    fig.colorbar(sc, ax=ax, shrink=0.6, label="Original L2 distance")
    ax.legend(loc="upper right")
    plt.tight_layout()

    saved = False
    base = None
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



def interactive_loop(
    conn: sqlite3.Connection,
    embedding_model,
    top_k: int,
    rewrite_mode: str,
    show_rewrite: bool,
    use_recency: bool,
    recency_half_life: float,
    recency_alpha: float,
) -> None:
    print("Entering interactive mode. Type 'exit', 'quit', or ':q' to leave.")
    history: List[dict] = []
    while True:
        try:
            question = input("\nYour question> ").strip()
        except (KeyboardInterrupt, EOFError):
            print("\nExiting interactive mode.")
            break
        if question.lower() in {"exit", "quit", ":q"}:
            print("Goodbye.")
            break
        if not question:
            continue
        answer, rewritten = answer_question(
            question,
            conn,
            embedding_model,
            top_k,
            rewrite_mode=rewrite_mode,
            show_rewrite=show_rewrite,
            history=history,
            use_recency=use_recency,
            recency_half_life=recency_half_life,
            recency_alpha=recency_alpha,
        )
        history.append({"question": question, "rewritten": rewritten, "answer": answer})


def run_demo(
    documents: Sequence[str] = DEFAULT_DOCUMENTS,
    question: Optional[str] = None,
    top_k: int = 3,
    rebuild: bool = True,
    interactive: bool = False,
    visualize: bool = False,
    visualize_file: Optional[str] = None,
    animate: bool = False,
    label_mode: str = "none",
    export_coords: Optional[str] = None,
    plotly_html: Optional[str] = None,
    rewrite_mode: str = "none",
    show_rewrite: bool = False,
    add_texts: Optional[List[str]] = None,
    use_recency: bool = False,
    recency_half_life: float = 7 * 24 * 3600.0,
    recency_alpha: float = 0.3,
) -> None:
    if rebuild:
        reset_database()
    conn = connect_sqlite()
    ensure_schema(conn)
    embedding_model = load_embedding_model()
    ensure_index(conn, embedding_model, documents, rebuild=rebuild)

    # Append user-provided texts (must happen before answering / visualization)
    if add_texts:
        add_text_documents(conn, embedding_model, add_texts)

    if interactive:
        interactive_loop(
            conn,
            embedding_model,
            top_k,
            rewrite_mode,
            show_rewrite,
            use_recency,
            recency_half_life,
            recency_alpha,
        )
    else:
        # Use default demo question if none supplied.
        q = question or "What is the energy source for a cell?"
        answer_question(
            q,
            conn,
            embedding_model,
            top_k,
            rewrite_mode=rewrite_mode,
            show_rewrite=show_rewrite,
            history=[],
            use_recency=use_recency,
            recency_half_life=recency_half_life,
            recency_alpha=recency_alpha,
        )
        if visualize:
            visualize_embeddings(
                conn,
                embedding_model,
                q,
                top_k=top_k,
                outfile=visualize_file,
                animate=animate,
                label_mode=label_mode,
                export_coords=export_coords,
                plotly_html=plotly_html,
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
    parser.add_argument(
        "--label-mode",
        choices=["none", "id", "short", "full"],
        default="none",
        help="How to label points in static plot: none, id, short (id+snippet), full (entire text).",
    )
    parser.add_argument(
        "--export-coords",
        type=str,
        help="Path to write a JSON file containing reduced coordinates & metadata.",
    )
    parser.add_argument(
        "--plotly-html",
        type=str,
        help="Generate an interactive Plotly HTML scatter with hover tooltips.",
    )
    parser.add_argument(
        "--rewrite-mode",
        choices=["none", "summarize", "expand", "disambiguate", "auto"],
        default="none",
        help="LLM-assisted query rewriting mode before embedding search.",
    )
    parser.add_argument(
        "--show-rewrite",
        action="store_true",
        help="Print the rewritten query used for retrieval when rewriting is enabled.",
    )
    parser.add_argument(
        "--add-text",
        action="append",
        metavar="TEXT",
        help="Append a raw text snippet as a new document (can be specified multiple times).",
    )
    parser.add_argument(
        "--use-recency",
        action="store_true",
        help="Enable recency-aware re-ranking (recent docs slightly favored).",
    )
    parser.add_argument(
        "--recency-half-life",
        type=float,
        default=7 * 24 * 3600.0,
        help="Half-life in seconds for recency decay (default 7 days).",
    )
    parser.add_argument(
        "--recency-alpha",
        type=float,
        default=0.3,
        help="Blend factor for recency (0 disables effect after enabling). Default 0.3",
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
        label_mode=args.label_mode,
        export_coords=args.export_coords,
        plotly_html=args.plotly_html,
        rewrite_mode=args.rewrite_mode,
        show_rewrite=args.show_rewrite,
        add_texts=args.add_text,
        use_recency=args.use_recency,
        recency_half_life=args.recency_half_life,
        recency_alpha=args.recency_alpha,
    )