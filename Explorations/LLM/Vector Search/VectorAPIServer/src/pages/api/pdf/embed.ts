import type { APIRoute } from 'astro';
import { requireAuth } from '../../../lib/auth';
import { GoogleGenAI } from '@google/genai';
import { GEMINI_API_KEY, GEMINI_GENERATION_MODEL, GEMINI_TASK_DOC } from '../../../lib/config';
import { embedText } from '../../../lib/embeddings';
import { insertDoc } from '../../../lib/db';
import { countTokens } from '../../../lib/tokens';

export const prerender = false;

/*
POST /api/pdf/embed
Body JSON: {
  file_uri: string (Gemini File API file.uri OR file name like files/abc-123),
  chunk_tokens?: number (default 1200),
  max_chunks?: number (optional cap),
  task_type?: string (embedding task),
  strategy?: 'full' | 'summary' (default 'full')
}
We ask Gemini model to extract textual chunks; then embed each and store in docs.
No local PDF parsing; relies on Gemini vision understanding of PDF.
*/

interface Chunk { index:number; text:string }

function extractJSON(raw: string): any | null {
  try { return JSON.parse(raw); } catch {}
  // attempt to find first { and last }
  const first = raw.indexOf('{'); const last = raw.lastIndexOf('}');
  if (first>=0 && last>first) {
    const sub = raw.slice(first, last+1);
    try { return JSON.parse(sub); } catch {}
  }
  return null;
}

export const POST: APIRoute = async ({ request }) => {
  const auth = requireAuth(request); if (auth instanceof Response) return auth;
  if (!GEMINI_API_KEY) return new Response(JSON.stringify({ error: 'Missing GEMINI_API_KEY' }), { status: 500 });
  try {
    const body = await request.json();
    const fileUriInput = (body.file_uri || '').trim();
    if (!fileUriInput) return new Response(JSON.stringify({ error: 'file_uri required'}), { status: 400 });
    const chunkTokens = Math.min(Math.max(parseInt(body.chunk_tokens || '1200', 10), 200), 4000);
    const maxChunks = body.max_chunks ? Math.max(1, parseInt(body.max_chunks, 10)) : undefined;
    const taskType = body.task_type || GEMINI_TASK_DOC;
    const strategy = body.strategy === 'summary' ? 'summary' : 'full';

    const fileUri = fileUriInput.startsWith('files/') || fileUriInput.startsWith('https://') ? fileUriInput : `files/${fileUriInput}`;

    const client = new GoogleGenAI({ apiKey: GEMINI_API_KEY });
    // Construct file part
    const filePart: any = { fileData: { fileUri, mimeType: 'application/pdf' } };
    const modeInstruction = strategy === 'summary'
      ? 'Provide a SMALL number of high-information chunks that together summarize the PDF.'
      : 'Extract the FULL textual content of the PDF as sequential chunks.';
    const prompt = `You are given a PDF. ${modeInstruction}\nSplit text into chunks each <= ${chunkTokens} tokens. Preserve logical order. Return STRICT JSON only with shape: {"chunks":[{"index":0,"text":"..."}, ...]} . Do NOT include markdown fences or commentary.`;
    const resp = await client.models.generateContent({ model: GEMINI_GENERATION_MODEL, contents: [filePart, { text: prompt }] });
    const raw = (resp.text || '').trim();
    const parsed = extractJSON(raw);
    if (!parsed || !Array.isArray(parsed.chunks)) {
      return new Response(JSON.stringify({ error: 'failed to parse model JSON', raw_preview: raw.slice(0,400)}), { status: 502 });
    }
    let chunks: Chunk[] = (parsed.chunks as any[])
      .filter((c: any) => c && typeof c.text === 'string')
      .map((c: any, i: number) => ({ index: typeof c.index === 'number' ? c.index : i, text: String(c.text).trim() }))
      .filter((c: Chunk) => c.text.length > 0);
    if (maxChunks && chunks.length > maxChunks) chunks = chunks.slice(0, maxChunks);
    if (!chunks.length) return new Response(JSON.stringify({ error: 'no chunks extracted'}), { status: 422 });

  const inserted: { id:number; index:number; token_count?:number }[] = [];
    for (const ch of chunks) {
      const tokens = await countTokens(ch.text).catch(()=>undefined);
      try {
        const emb = await embedText(ch.text, { taskType, isQuery: false });
        const id = insertDoc(ch.text, emb, undefined, undefined, undefined, undefined, taskType, undefined, tokens || undefined); // summary/tags optional later
  inserted.push({ id, index: ch.index, token_count: (tokens==null ? undefined : tokens) });
      } catch (e:any) {
        // continue; could add partial failure info
      }
    }
    return new Response(JSON.stringify({ inserted_count: inserted.length, chunks: inserted, task_type: taskType, strategy }), { status: 201 });
  } catch (e:any) {
    return new Response(JSON.stringify({ error: e.message || 'pdf embed failed'}), { status: 400 });
  }
};