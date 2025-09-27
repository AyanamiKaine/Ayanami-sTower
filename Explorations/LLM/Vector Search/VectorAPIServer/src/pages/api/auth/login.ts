import type { APIRoute } from 'astro';
import { loginUser, setSessionCookie } from '../../../lib/auth';

export const prerender = false;

export const POST: APIRoute = async ({ request }) => {
  try {
    const body = await request.json();
    if (!body?.email || !body?.password) return new Response(JSON.stringify({ error: 'email and password required'}), { status: 400 });
    const { token, user } = await loginUser(body.email.toLowerCase().trim(), body.password);
    const cookie = setSessionCookie(token);
    return new Response(JSON.stringify({ token, user: { id: user.id, email: user.email, is_admin: !!user.is_admin } }), { status: 200, headers: { 'Set-Cookie': cookie } });
  } catch (e:any) {
    return new Response(JSON.stringify({ error: e.message }), { status: 401 });
  }
};
