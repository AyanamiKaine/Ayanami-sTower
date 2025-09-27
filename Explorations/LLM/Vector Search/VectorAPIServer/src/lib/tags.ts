import { GoogleGenAI } from '@google/genai';
import { GEMINI_API_KEY, GEMINI_GENERATION_MODEL } from './config';

let _client: GoogleGenAI | null = null;
function client() {
  if (_client) return _client;
  if (!GEMINI_API_KEY) throw new Error('Missing GOOGLE_API_KEY');
  _client = new GoogleGenAI({ apiKey: GEMINI_API_KEY });
  return _client;
}

export interface TagOptions { maxTags?: number; }

export async function generateTags(text: string, opts: TagOptions = {}): Promise<string[]> {
  const maxTags = Math.min(Math.max(opts.maxTags ?? 6, 1), 20);
  const prompt = `Extract up to ${maxTags} short, lowercase, single or hyphenated keyword tags (no explanations) representing the core topics, entities, domains, and concepts from the document. Output only a comma-separated list.\n\n---\n${text}\n---\nTags:`;
  const c = client();
  const resp = await c.models.generateContent({ model: GEMINI_GENERATION_MODEL, contents: prompt });
  const raw = (resp.text || '').toLowerCase();
  const pieces = raw.split(/[,\n]/).map(t => t.trim().replace(/^[#\-]+/,'')).filter(Boolean);
  // Deduplicate preserving order
  const seen = new Set<string>();
  const tags: string[] = [];
  for (const p of pieces) {
    if (!seen.has(p)) { seen.add(p); tags.push(p); }
    if (tags.length >= maxTags) break;
  }
  return tags;
}
