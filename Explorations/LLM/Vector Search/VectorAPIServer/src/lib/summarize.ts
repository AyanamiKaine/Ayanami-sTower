import { GoogleGenAI } from '@google/genai';
import { GEMINI_API_KEY, GEMINI_GENERATION_MODEL } from './config';

let _client: GoogleGenAI | null = null;
function client() {
  if (_client) return _client;
  if (!GEMINI_API_KEY) throw new Error('Missing GOOGLE_API_KEY');
  _client = new GoogleGenAI({ apiKey: GEMINI_API_KEY });
  return _client;
}

export interface SummarizeOptions { targetTokens?: number; }

export async function generateSummary(text: string, opts: SummarizeOptions = {}): Promise<string> {
  const tokens = opts.targetTokens ?? 80;
  const prompt = `Summarize the following document in about ${tokens} tokens. Preserve key entities and dates.\n\n---\n${text}\n---\nSummary:`;
  const c = client();
  const resp = await c.models.generateContent({ model: GEMINI_GENERATION_MODEL, contents: prompt });
  const summary = (resp.text || '').trim().split('\n').filter(l=>l.trim()).slice(0,5).join(' ');
  return summary;
}
