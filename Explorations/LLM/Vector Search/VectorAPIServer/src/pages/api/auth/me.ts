import type { APIRoute } from 'astro';
import { getAuthUser } from '../../../lib/auth';

export const prerender = false;

export const GET: APIRoute = async ({ request }) => {
  const user = getAuthUser(request);
  if (!user) return new Response(JSON.stringify({ user: null }), { status: 200 });
  return new Response(JSON.stringify({ user }), { status: 200 });
};
