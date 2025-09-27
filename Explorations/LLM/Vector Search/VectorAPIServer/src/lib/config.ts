export const DB_FILE = process.env.VECTOR_DB_FILE || 'vector_memory_vec.db';
export const GEMINI_API_KEY = process.env.GEMINI_API || process.env.GEMINI_API_KEY || '';
export const GEMINI_EMBEDDING_MODEL = process.env.GEMINI_EMBEDDING_MODEL || 'gemini-embedding-001';
export const GEMINI_EMBEDDING_DIM = parseInt(process.env.GEMINI_EMBEDDING_DIM || '768', 10);
export const GEMINI_TASK_DOC = process.env.GEMINI_TASK_DOC || 'RETRIEVAL_DOCUMENT';
export const GEMINI_TASK_QUERY = process.env.GEMINI_TASK_QUERY || 'RETRIEVAL_QUERY';
export const GEMINI_GENERATION_MODEL = process.env.GEMINI_MODEL_NAME || 'gemini-2.5-flash-lite';

export const RECENCY_DEFAULT_HALF_LIFE = 7 * 24 * 3600; // seconds
export const RECENCY_DEFAULT_ALPHA = 0.3;
