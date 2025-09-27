import type { APIRoute } from 'astro';
import { listDocs } from '../..//lib/db';
import { requireAuth } from '../../lib/auth';

export const prerender = false;

export const GET: APIRoute = async ({ request }) => {
  const auth = requireAuth(request); if (auth instanceof Response) return auth;
  const url = new URL(request.url);
  const limit = url.searchParams.get('limit');
  const offset = url.searchParams.get('offset');
  const order = url.searchParams.get('order');
  const docs = listDocs({
    limit: limit ? parseInt(limit,10) : undefined,
    offset: offset ? parseInt(offset,10) : undefined,
    order: order === 'recent' ? 'recent' : 'id'
  });
  return new Response(JSON.stringify({ count: docs.length, docs }), { status: 200 });
};
