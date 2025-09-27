import { GoogleGenAI } from '@google/genai';
import { GEMINI_API_KEY, GEMINI_GENERATION_MODEL } from './config';

let _client: GoogleGenAI | null = null;
function getClient(): GoogleGenAI {
  if (_client) return _client;
  const key = GEMINI_API_KEY;
  if (!key) throw new Error('Missing GOOGLE_API_KEY / GEMINI_API_KEY');
  _client = new GoogleGenAI({ apiKey: key });
  return _client;
}

export async function countTokens(text: string): Promise<number | null> {
  try {
    const client = getClient();
    const res: any = await client.models.countTokens({ model: GEMINI_GENERATION_MODEL, contents: text });
    if (res && typeof res.totalTokens === 'number') return res.totalTokens;
    // fallback: usage metadata style
    if (res?.usageMetadata?.totalTokenCount) return res.usageMetadata.totalTokenCount;
    return null;
  } catch {
    return null;
  }
}
