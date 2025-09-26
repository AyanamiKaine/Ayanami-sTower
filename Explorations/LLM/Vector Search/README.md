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

Basic run:

```pwsh
python VectorSearchExample.py --question "What powers a cell?"
```

Interactive mode (ask multiple questions):

```pwsh
python VectorSearchExample.py -i
```

Reuse existing vector DB (skip re-embedding):

```pwsh
python VectorSearchExample.py --no-rebuild -q "Where is the Louvre?"
```

Load your own documents from a file (one line = one document):

```pwsh
python VectorSearchExample.py --docs-file my_corpus.txt -i
```

What happens on each run:

1. Optionally (re)builds the `vector_memory_vec.db` file (unless `--no-rebuild`).
2. Embeds documents with Sentence Transformers and stores float32 vectors in SQLite.
3. Embeds your query, retrieves nearest neighbours via `vec_distance_l2`.
4. Builds a RAG prompt and calls Gemini using the modern `google-genai` SDK (if API key present).
5. Falls back gracefully if Gemini isn't configured, still showing retrieved context.

## Customising

-   Replace `DEFAULT_DOCUMENTS` or provide `--docs-file`.
-   Use `--no-rebuild` to append new docs logic later (currently full rebuild deletes docs).
-   Extend `answer_question` for streaming (`client.models.generate_content_stream`).
-   Add function calling or JSON schema outputs by passing a `config` dict to the `generate_content` call.

## Visualization

You can visualize embeddings (reduced to 3D with PCA) relative to the query at the origin.

Examples:

```pwsh
python VectorSearchExample.py -q "What is photosynthesis?" --visualize
```

Save to a file:

```pwsh
python VectorSearchExample.py -q "What is photosynthesis?" --visualize --viz-file embeddings.png
```

Generate a rotating GIF (needs `pillow`):

```pwsh
python VectorSearchExample.py -q "What is photosynthesis?" --animate --viz-file embeddings.png
```

Install extra deps if you don't have them:

```pwsh
pip install matplotlib scikit-learn pillow
```

Top-k nearest documents are highlighted in red; the query appears as a black star at (0,0,0). Color encodes distance in the reduced space.

Advanced options:

```pwsh
# Label points with IDs and short text snippets
python VectorSearchExample.py -q "What is photosynthesis?" -v --label-mode short

# Export reduced coordinates + metadata to JSON
python VectorSearchExample.py -q "What is photosynthesis?" -v --export-coords coords.json

# Produce interactive Plotly HTML (hover to inspect)
python VectorSearchExample.py -q "What is photosynthesis?" -v --plotly-html embeddings.html

# All together (static PNG, GIF, Plotly, JSON, labels)
python VectorSearchExample.py -q "What is photosynthesis?" --visualize \
	--viz-file embeddings.png --animate --label-mode short \
	--export-coords coords.json --plotly-html embeddings.html
```

Label modes:

-   none: no labels (default, least clutter)
-   id: only the numeric doc id
-   short: doc id + truncated snippet
-   full: full text (useful for small corpora only)

Files produced:

-   embeddings.png (static plot)
-   embeddings_rotation.gif (if --animate)
-   embeddings.html (if --plotly-html)
-   coords.json (if --export-coords)

The JSON export contains each point's reduced coords, distance, and original text so you can build custom dashboards.
