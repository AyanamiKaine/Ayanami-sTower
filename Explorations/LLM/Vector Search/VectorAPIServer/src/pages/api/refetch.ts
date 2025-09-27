import type { APIRoute } from 'astro';
import { getDoc, updateDoc, updateDocSummary, updateDocTags } from '../../lib/db';
import { fetchPageText } from '../../lib/web';
import { embedText } from '../../lib/embeddings';
import { generateSummary } from '../../lib/summarize';
import { generateTags } from '../../lib/tags';
import { countTokens } from '../../lib/tokens';
import { requireAuth } from '../../lib/auth';

export const prerender = false;

// POST /api/refetch { id, resummarize?, retag?, summary_tokens?, task_type? }
// Re-downloads URL content for doc id (must have url) and re-embeds text.
export const POST: APIRoute = async ({ request }) => {
  const auth = requireAuth(request); if (auth instanceof Response) return auth;
  try {
    const body = await request.json();
    const id = body.id;
    if (id == null) return new Response(JSON.stringify({ error: 'id required'}), { status: 400 });
    const doc = getDoc(Number(id));
    if (!doc) return new Response(JSON.stringify({ error: 'not found'}), { status: 404 });
    if (!doc.url) return new Response(JSON.stringify({ error: 'doc has no url'}), { status: 400 });
    const taskType = body.task_type || doc.embedding_task;
    let text: string;
    try { text = await fetchPageText(doc.url); } catch (e:any) { return new Response(JSON.stringify({ error: e.message || 'fetch failed'}), { status: 502 }); }
    if (!text) return new Response(JSON.stringify({ error: 'empty extracted'}), { status: 422 });
    const MAX_CHARS = 120_000;
    if (text.length > MAX_CHARS) text = text.slice(0, MAX_CHARS) + `\n[truncated length=${text.length}]`;

  const embedding = await embedText(text, { isQuery: false, taskType });
  const tokenCount = await countTokens(text);
  updateDoc(doc.id, text, embedding, taskType, tokenCount ?? undefined);

    let summary: string | undefined;
    if (body.resummarize) {
      const tokens = body.summary_tokens ?? 80;
      try {
        summary = await generateSummary(text, { targetTokens: tokens });
        const summaryEmbedding = await embedText(summary, { isQuery: false, taskType });
        updateDocSummary(doc.id, summary, summaryEmbedding);
      } catch {}
    }

    let tags: string[] | undefined;
    if (body.retag) {
      try { tags = await generateTags(text, { maxTags: body.max_tags ?? 6 }); updateDocTags(doc.id, tags); } catch {}
    }

  return new Response(JSON.stringify({ id: doc.id, url: doc.url, text, summary, tags, embedding_task: taskType, token_count: tokenCount }), { status: 200 });
  } catch (e:any) {
    return new Response(JSON.stringify({ error: e.message || 'failed'}), { status: 500 });
  }
};
