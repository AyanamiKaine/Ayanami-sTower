import type { APIRoute } from 'astro';
import { registerUser } from '../../../lib/auth';
import { hasAdmin, getUserById } from '../../../lib/db';

export const prerender = false;

export const POST: APIRoute = async ({ request }) => {
  try {
    const body = await request.json();
    if (!body?.email || !body?.password) return new Response(JSON.stringify({ error: 'email and password required'}), { status: 400 });
  const existed = hasAdmin();
  const id = await registerUser(body.email.toLowerCase().trim(), body.password);
  const u = getUserById(id)!;
  return new Response(JSON.stringify({ id, is_admin: !!u.is_admin, is_approved: !!u.is_approved, status: u.is_approved ? 'ready' : 'pending_approval' }), { status: 201 });
  } catch (e:any) {
    return new Response(JSON.stringify({ error: e.message }), { status: 400 });
  }
};
