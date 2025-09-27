import type { APIRoute } from 'astro';
import { embedText } from '../../lib/embeddings';
import { insertDoc, getDoc, updateDoc, deleteDoc, updateDocSummary, updateDocTags } from '../../lib/db';
import { generateSummary } from '../../lib/summarize';
import { generateTags } from '../../lib/tags';

export const prerender = false; // dynamic API (DB + embeddings)

export const GET: APIRoute = async ({ request }) => {
  const url = new URL(request.url);
  const idParam = url.searchParams.get('id');
  if (!idParam) return new Response(JSON.stringify({ error: 'id required'}), { status: 400 });
  const id = parseInt(idParam, 10);
  const doc = getDoc(id);
  if (!doc) return new Response(JSON.stringify({ error: 'not found'}), { status: 404 });
  return new Response(JSON.stringify({ id: doc.id, text: doc.text, embedding_task: doc.embedding_task, created_at: doc.created_at, updated_at: doc.updated_at }), { status: 200 });
};

export const POST: APIRoute = async ({ request }) => {
  const body = await request.json();
  if (!body?.text) return new Response(JSON.stringify({ error: 'text required' }), { status: 400 });
  const embedding = await embedText(body.text, { isQuery: false, taskType: body.task_type });
  let summary: string | undefined;
  let summaryEmbedding: Float32Array | null = null;
  let tags: string[] | undefined;
  if (body.auto_summarize) {
    try { 
      summary = await generateSummary(body.text, { targetTokens: body.summary_tokens }); 
  summaryEmbedding = await embedText(summary, { isQuery: false, taskType: body.task_type });
    } catch {}
  }
  if (body.auto_tag) {
    try { tags = await generateTags(body.text, { maxTags: body.max_tags }); } catch {}
  }
  const id = insertDoc(body.text, embedding, body.id, summary, summaryEmbedding, tags, body.task_type);
  return new Response(JSON.stringify({ id, text: body.text, summary, tags, embedding_task: body.task_type }), { status: 201 });
};

export const PUT: APIRoute = async ({ request }) => {
  const body = await request.json();
  if (body?.id == null || !body?.text) return new Response(JSON.stringify({ error: 'id and text required' }), { status: 400 });
  const embedding = await embedText(body.text, { isQuery: false, taskType: body.task_type });
  try {
  updateDoc(body.id, body.text, embedding, body.task_type);
    if (body.resummarize) {
      try {
        const summary = await generateSummary(body.text, { targetTokens: body.summary_tokens });
        let summaryEmbedding: Float32Array | undefined;
  try { summaryEmbedding = await embedText(summary, { isQuery: false, taskType: body.task_type }); } catch {}
        updateDocSummary(body.id, summary, summaryEmbedding);
      } catch {}
    }
    if (body.retag) {
      try { const tags = await generateTags(body.text, { maxTags: body.max_tags }); updateDocTags(body.id, tags); } catch {}
    }
  return new Response(JSON.stringify({ id: body.id, text: body.text, embedding_task: body.task_type }), { status: 200 });
  } catch (e: any) {
    return new Response(JSON.stringify({ error: e.message }), { status: 404 });
  }
};

// PATCH semantics:
//  - if body.action == 'summarize' -> regenerate summary
//  - if body.action == 'tag'       -> regenerate tags
export const PATCH: APIRoute = async ({ request }) => {
  const url = new URL(request.url);
  const idParam = url.searchParams.get('id');
  if (!idParam) return new Response(JSON.stringify({ error: 'id required'}), { status: 400 });
  const id = parseInt(idParam, 10);
  const doc = getDoc(id);
  if (!doc) return new Response(JSON.stringify({ error: 'not found'}), { status: 404 });
  try {
    const body = await request.json().catch(()=>({}));
    const action = body.action || 'summarize';
    if (action === 'summarize') {
      const tokens = body.summary_tokens ?? 80;
      const summary = await generateSummary(doc.text.toString(), { targetTokens: tokens });
      let summaryEmbedding: Float32Array | undefined;
  try { summaryEmbedding = await embedText(summary, { isQuery: false, taskType: body.task_type }); } catch {}
      updateDocSummary(id, summary, summaryEmbedding);
      return new Response(JSON.stringify({ id, summary }), { status: 200 });
    } else if (action === 'tag') {
      const tags = await generateTags(doc.text.toString(), { maxTags: body.max_tags });
      updateDocTags(id, tags);
      return new Response(JSON.stringify({ id, tags }), { status: 200 });
    } else {
      return new Response(JSON.stringify({ error: 'unknown action'}), { status: 400 });
    }
  } catch (e:any) {
    return new Response(JSON.stringify({ error: e.message }), { status: 500 });
  }
};

export const DELETE: APIRoute = async ({ request }) => {
  const url = new URL(request.url);
  const idParam = url.searchParams.get('id');
  if (!idParam) return new Response(JSON.stringify({ error: 'id required'}), { status: 400 });
  try { deleteDoc(parseInt(idParam, 10)); return new Response(null, { status: 204 }); } catch { return new Response(JSON.stringify({ error: 'not found'}), { status: 404 }); }
};
