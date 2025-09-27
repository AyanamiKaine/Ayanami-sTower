import type { APIRoute } from 'astro';
import { embedText } from '../../lib/embeddings';
import { search } from '../../lib/db';
import { rewriteQuery } from '../../lib/rewrite';
import { RECENCY_DEFAULT_ALPHA, RECENCY_DEFAULT_HALF_LIFE } from '../../lib/config';
import { requireAuth } from '../../lib/auth';

export const prerender = false;

export const POST: APIRoute = async ({ request }) => {
  const auth = requireAuth(request); if (auth instanceof Response) return auth;
  const body = await request.json();
  const query = body.query as string;
  if (!query) return new Response(JSON.stringify({ error: 'query required'}), { status: 400 });
  const top_k = body.top_k ? Math.min(parseInt(body.top_k, 10), 50) : 5;
  const rewrite_mode = body.rewrite_mode || 'none';
  const use_recency = !!body.use_recency;
  const half_life = body.recency_half_life || RECENCY_DEFAULT_HALF_LIFE;
  const alpha = body.recency_alpha ?? RECENCY_DEFAULT_ALPHA;
  const use_summary = !!body.use_summary;
  const rewritten = await rewriteQuery(query, rewrite_mode);
  const max_distance = body.max_distance != null ? Number(body.max_distance) : null;
  const embedding = await embedText(rewritten !== query ? rewritten : query, { isQuery: true });
  let results = search(embedding, top_k, use_recency, half_life, alpha, { useSummary: use_summary, fallbackFull: true }).map(r => ({
    id: r.id,
    text: r.text,
    summary: r.summary,
    tags: r.tags,
    embedding_task: r.embedding_task,
    url: r.url,
  token_count: r.token_count,
    distance: r.distance,
    created_at: r.created_at,
    updated_at: r.updated_at,
  }));
  if (max_distance != null && !Number.isNaN(max_distance)) {
    results = results.filter(r => r.distance <= max_distance);
  }
  return new Response(JSON.stringify({ query, rewritten_query: rewritten, top_k, using_summary: use_summary, max_distance, results }), { status: 200 });
};
