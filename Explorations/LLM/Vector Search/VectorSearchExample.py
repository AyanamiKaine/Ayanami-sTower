import sqlite3
import numpy as np
from sentence_transformers import SentenceTransformer
import sqlite_vec
import os # Import the 'os' module to interact with the operating system

# --- NEW: ADD THIS SECTION TO ENSURE A CLEAN DATABASE ---
DB_FILE = 'vector_memory_vec.db'
if os.path.exists(DB_FILE):
    print(f"Removing existing database file: {DB_FILE}")
    os.remove(DB_FILE)
# --- END OF NEW SECTION ---


# --- 1. SETUP: MODEL and DATA ---
model = SentenceTransformer('all-MiniLM-L6-v2')

documents = [
    "The capital of France is Paris.",
    "Mitochondria are the powerhouse of the cell.",
    "The 2024 Summer Olympics were held in the French capital.",
    "Photosynthesis is the process used by plants to convert light energy into chemical energy.",
    "The Louvre Museum, located in Paris, is the world's largest art museum.",
    "Cellular respiration releases energy from glucose."
]

# --- 2. EMBEDDING and INDEXING ---
print("Generating embeddings for documents...")
doc_embeddings = model.encode(documents)

# Connect to the database (it will now always be a new file)
conn = sqlite3.connect(DB_FILE)

conn.enable_load_extension(True)
sqlite_vec.load(conn)
conn.enable_load_extension(False)

vec_version, = conn.execute("select vec_version()").fetchone()
print(f"sqlite-vec version: {vec_version}")

conn.execute('CREATE TABLE IF NOT EXISTS docs(id INTEGER PRIMARY KEY, text TEXT, embedding BLOB)')

print("Storing documents and embeddings in SQLite...")
for i, (doc, emb) in enumerate(zip(documents, doc_embeddings)):
    emb_float32 = emb.astype(np.float32)
    # This insert will now always succeed because the table is always empty at this point
    conn.execute('INSERT INTO docs(id, text, embedding) VALUES (?, ?, ?)', (i + 1, doc, emb_float32))

conn.commit()
print("Database created and indexed successfully!")


# --- 3. QUERYING and RETRIEVAL ---
print("\n--- Performing a search ---")
query = "What is the energy source for a cell?"
print(f"Query: '{query}'")

query_embedding = model.encode(query).astype(np.float32)

cursor = conn.execute(
    """
    SELECT
        id,
        text,
        vec_distance_l2(embedding, ?) as distance
    FROM docs
    ORDER BY distance
    LIMIT 3
    """,
    (query_embedding,)
)

results = cursor.fetchall()

print("\nTop 3 most similar results from memory:")
for rowid, doc_text, distance in results:
    print(f"  - (ID: {rowid}, Distance: {distance:.4f}): '{doc_text}'")

conn.close()