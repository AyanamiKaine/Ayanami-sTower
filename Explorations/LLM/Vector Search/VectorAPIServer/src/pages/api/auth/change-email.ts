import type { APIRoute } from 'astro';
import { requireAuth, changeEmail } from '../../../lib/auth';

export const prerender = false;

export const POST: APIRoute = async ({ request }) => {
  const auth = requireAuth(request);
  if (auth instanceof Response) return auth;
  try {
    const body = await request.json();
    if (!body?.current_password || !body?.new_email) {
      return new Response(JSON.stringify({ error: 'current_password and new_email required'}), { status: 400 });
    }
    await changeEmail(auth.userId, body.current_password, body.new_email);
    return new Response(JSON.stringify({ ok: true, new_email: body.new_email.toLowerCase().trim() }), { status: 200 });
  } catch(e:any) {
    return new Response(JSON.stringify({ error: e.message }), { status: 400 });
  }
};
