import type { APIRoute } from 'astro';
import { countDocs } from '../../lib/db';
import { GEMINI_EMBEDDING_MODEL, GEMINI_EMBEDDING_DIM } from '../../lib/config';
import { requireAuth } from '../../lib/auth';

export const prerender = false;

export const GET: APIRoute = async ({ request }) => {
  const auth = requireAuth(request); if (auth instanceof Response) return auth;
  const documents = countDocs();
  return new Response(JSON.stringify({ documents, embedding_backend: 'gemini', embedding_model: GEMINI_EMBEDDING_MODEL, embedding_dimension: GEMINI_EMBEDDING_DIM }), { status: 200 });
};
