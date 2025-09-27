import type { APIRoute } from 'astro';
import { requireAuth, changePassword, verifyPassword } from '../../../lib/auth';
import { getUserById } from '../../../lib/db';

export const prerender = false;

export const POST: APIRoute = async ({ request }) => {
  const auth = requireAuth(request);
  if (auth instanceof Response) return auth;
  try {
    const body = await request.json();
    const current = body.current_password as string | undefined;
    const next = body.new_password as string | undefined;
    if (!next || next.length < 6) return new Response(JSON.stringify({ error: 'new password min length 6'}), { status: 400 });
    const user = getUserById(auth.userId);
    if (!user) return new Response(JSON.stringify({ error: 'user not found'}), { status: 404 });
    // Require current password for safety unless user has just been auto-created (still require anyway)
    if (!current) return new Response(JSON.stringify({ error: 'current_password required'}), { status: 400 });
    const valid = await verifyPassword(current, user.password_hash);
    if (!valid) return new Response(JSON.stringify({ error: 'invalid current password'}), { status: 400 });
    await changePassword(user.id, next);
    return new Response(JSON.stringify({ ok: true }), { status: 200 });
  } catch(e:any) {
    return new Response(JSON.stringify({ error: e.message || 'failed'}), { status: 500 });
  }
};
