# Vector Search + Gemini Demo

This example shows how to build a tiny retrieval-augmented generation (RAG) workflow that stores sentence embeddings in SQLite using the [`sqlite-vec`](https://github.com/sqlite/sqlite-vec) extension and sends the retrieved context to the Gemini API for final answer generation.

## Prerequisites

1. **Python 3.10+**
2. Install the Python dependencies:

```pwsh
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -r requirements.txt
```

3. Obtain a [Google AI Studio](https://ai.google.dev/) API key and export it (PowerShell):

```pwsh
$env:GOOGLE_API_KEY = "<your-key>"
```

You can optionally override the embedding or Gemini model by setting `EMBEDDING_MODEL_NAME` or `GEMINI_MODEL_NAME` environment variables.

## Run the demo

```pwsh
python VectorSearchExample.py
```

What happens:

1. The script resets the `vector_memory_vec.db` file and loads `sqlite-vec`.
2. Documents are embedded with Sentence Transformers and stored as float32 vectors in SQLite.
3. A query is embedded, the nearest neighbours are retrieved, and a Gemini prompt is assembled from the results.
4. If `GOOGLE_API_KEY` is set and `google-generativeai` is installed, the Gemini model answers the question using the retrieved context. If not, the script prints the context-only fallback message.

## Customising

-   Replace `DEFAULT_DOCUMENTS` in `VectorSearchExample.py` with your own corpus.
-   Persist the database and skip `reset_database()` when you want updates instead of rebuilds.
-   Wrap the `run_demo` function or copy the helper functions into your own application to build an API or chat workflow on top of the same components.
