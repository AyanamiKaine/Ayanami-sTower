import type { APIRoute } from 'astro';
import { hasAdmin } from '../../../lib/db';

export const prerender = false;

export const GET: APIRoute = async () => {
  return new Response(JSON.stringify({ has_admin: hasAdmin() }), { status: 200 });
};
