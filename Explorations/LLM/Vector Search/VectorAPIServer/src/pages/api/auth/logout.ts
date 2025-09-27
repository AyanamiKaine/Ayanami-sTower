import type { APIRoute } from 'astro';
import { extractToken, logoutUser } from '../../../lib/auth';

export const prerender = false;

export const POST: APIRoute = async ({ request }) => {
  const token = extractToken(request);
  if (token) logoutUser(token);
  return new Response(JSON.stringify({ ok: true }), { status: 200, headers: { 'Set-Cookie': 'session=; Max-Age=0; Path=/' } });
};
