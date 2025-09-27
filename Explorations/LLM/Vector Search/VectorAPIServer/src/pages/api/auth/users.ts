import type { APIRoute } from 'astro';
import { requireAuth, adminListUsers, adminApproveUser, adminDeleteUser } from '../../../lib/auth';

export const prerender = false;

export const GET: APIRoute = async ({ request }) => {
  const auth = requireAuth(request);
  if (auth instanceof Response) return auth;
  if (!auth.isAdmin) return new Response(JSON.stringify({ error: 'forbidden'}), { status: 403 });
  return new Response(JSON.stringify({ users: adminListUsers() }), { status: 200 });
};

export const POST: APIRoute = async ({ request }) => {
  const auth = requireAuth(request);
  if (auth instanceof Response) return auth;
  if (!auth.isAdmin) return new Response(JSON.stringify({ error: 'forbidden'}), { status: 403 });
  try {
    const body = await request.json();
    if (body.action === 'approve' && body.id) {
      adminApproveUser(body.id);
      return new Response(JSON.stringify({ ok: true, action: 'approve' }), { status: 200 });
    } else if (body.action === 'delete' && body.id) {
      adminDeleteUser(body.id);
      return new Response(JSON.stringify({ ok: true, action: 'delete' }), { status: 200 });
    } else if (body.action === 'disapprove' && body.id) {
      // Prevent disapproving admin accounts to avoid accidental lock-out
      const { getDB } = await import('../../../lib/db');
      const db = getDB();
      const user = db.prepare('SELECT id,is_admin FROM users WHERE id=?').get(body.id) as any;
      if (!user) return new Response(JSON.stringify({ error: 'user not found'}), { status: 404 });
      if (user.is_admin) return new Response(JSON.stringify({ error: 'cannot disapprove admin'}), { status: 400 });
      db.prepare('UPDATE users SET is_approved=0 WHERE id=?').run(body.id);
      return new Response(JSON.stringify({ ok: true, action: 'disapprove' }), { status: 200 });
    }
    return new Response(JSON.stringify({ error: 'unknown action' }), { status: 400 });
  } catch (e:any) {
    return new Response(JSON.stringify({ error: e.message }), { status: 400 });
  }
};
