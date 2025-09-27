import { GoogleGenAI } from '@google/genai';
import { GEMINI_API_KEY, GEMINI_EMBEDDING_MODEL, GEMINI_EMBEDDING_DIM, GEMINI_TASK_DOC, GEMINI_TASK_QUERY } from './config';

let _client: GoogleGenAI | null = null;

function getClient(): GoogleGenAI {
  if (_client) return _client;
  const key = GEMINI_API_KEY;
  if (!key) throw new Error('Missing GOOGLE_API_KEY / GEMINI_API_KEY');
  _client = new GoogleGenAI({ apiKey: key });
  return _client;
}

function normalize(vec: number[]): number[] {
  const norm = Math.sqrt(vec.reduce((s, v) => s + v * v, 0));
  if (norm === 0) return vec;
  return vec.map(v => v / norm);
}

export interface EmbedOptions { isQuery?: boolean; taskType?: string }

export async function embedTexts(texts: string[], opts: EmbedOptions = {}): Promise<Float32Array[]> {
  if (!texts.length) return [];
  const taskType = opts.taskType || (opts.isQuery ? GEMINI_TASK_QUERY : GEMINI_TASK_DOC);
  const client = getClient();
  const params: any = {
    model: GEMINI_EMBEDDING_MODEL,
    contents: texts,
    taskType,
    outputDimensionality: GEMINI_EMBEDDING_DIM,
  };
  const res = await client.models.embedContent(params as any);
  return (res.embeddings || []).map((e: any) => {
    let values: number[] = e.values as number[];
    if (GEMINI_EMBEDDING_DIM !== 3072) values = normalize(values);
    return new Float32Array(values);
  });
}

export async function embedText(text: string, opts: EmbedOptions = {}): Promise<Float32Array> {
  const [v] = await embedTexts([text], opts);
  return v;
}
