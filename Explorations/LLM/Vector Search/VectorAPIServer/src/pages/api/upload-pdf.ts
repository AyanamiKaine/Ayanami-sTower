import type { APIRoute } from 'astro';
import { requireAuth } from '../../lib/auth';
import { GoogleGenAI } from '@google/genai';
import { GEMINI_API_KEY } from '../../lib/config';

export const prerender = false;

// Upload a PDF file directly to Gemini File API and return file metadata
// Expects multipart/form-data with 'file' (application/pdf) and optional 'display_name'
// Does NOT embed or store content locally; returns Gemini file handle for later use.

export const POST: APIRoute = async ({ request }) => {
  const auth = requireAuth(request); if (auth instanceof Response) return auth;
  try {
    const ct = request.headers.get('content-type') || '';
    if (!ct.includes('multipart/form-data')) {
      return new Response(JSON.stringify({ error: 'multipart/form-data required' }), { status: 400 });
    }
    if (!GEMINI_API_KEY) return new Response(JSON.stringify({ error: 'Missing GEMINI_API_KEY' }), { status: 500 });
    const form = await request.formData();
    const file = form.get('file');
    if (!(file instanceof File)) return new Response(JSON.stringify({ error: 'file required'}), { status: 400 });
    if (!file.name.toLowerCase().endsWith('.pdf')) return new Response(JSON.stringify({ error: 'only .pdf allowed here'}), { status: 415 });
    const displayName = (form.get('display_name') as string) || file.name;

    const maxBytes = 50 * 1024 * 1024; // 50MB per Gemini File API docs
    if (file.size > maxBytes) {
      return new Response(JSON.stringify({ error: 'PDF exceeds 50MB File API limit' }), { status: 400 });
    }

    const arrayBuf = await file.arrayBuffer();
    const nodeBuf = Buffer.from(arrayBuf);

    // Strategy 1: SDK object style argument
    let uploaded: any = null;
    let lastError: any = null;
    try {
      const client = new GoogleGenAI({ apiKey: GEMINI_API_KEY });
      uploaded = await (client as any).files.upload({ file: nodeBuf, mimeType: 'application/pdf', displayName });
    } catch (e:any) {
      lastError = e;
      // Strategy 2: SDK positional arguments (file first, then options)
      try {
        const client2 = new GoogleGenAI({ apiKey: GEMINI_API_KEY });
        uploaded = await (client2 as any).files.upload(nodeBuf, { mimeType: 'application/pdf', displayName });
      } catch (e2:any) {
        lastError = e2;
      }
    }

    // Strategy 3: Raw HTTP upload (media upload endpoint) if SDK attempts failed
    if (!uploaded) {
      try {
        const res = await fetch('https://generativelanguage.googleapis.com/upload/v1beta/files', {
          method: 'POST',
          headers: {
            'x-goog-api-key': GEMINI_API_KEY,
            'Content-Type': 'application/pdf'
          },
          body: nodeBuf
        });
        const j = await res.json().catch(()=>null);
        if (!res.ok) {
          return new Response(JSON.stringify({ error: j?.error?.message || j?.error || 'raw upload failed', stage: 'raw', status: res.status }), { status: 400 });
        }
        // Raw upload returns { file: { ... } }
        return new Response(JSON.stringify({ file: j.file }), { status: 201 });
      } catch (e3:any) {
        return new Response(JSON.stringify({ error: e3.message || 'upload failed', stage: 'raw-exception', previous: lastError?.message }), { status: 400 });
      }
    }

    // Normalize response shape
    const fileMeta = uploaded.file || uploaded; // depending on SDK return format
    if (!fileMeta || !fileMeta.name) {
      return new Response(JSON.stringify({ error: 'unexpected upload response', debug: Object.keys(uploaded || {}) }), { status: 400 });
    }
    return new Response(JSON.stringify({ file: fileMeta }), { status: 201 });
  } catch (e:any) {
    return new Response(JSON.stringify({ error: e.message || 'upload failed'}), { status: 400 });
  }
};