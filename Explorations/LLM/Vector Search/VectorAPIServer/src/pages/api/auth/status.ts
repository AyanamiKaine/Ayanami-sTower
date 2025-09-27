import type { APIRoute } from 'astro';
import { hasAdmin, ensureAdminsApproved } from '../../../lib/db';

export const prerender = false;

export const GET: APIRoute = async () => {
  const before = hasAdmin();
  let healed = 0;
  if (before) {
    healed = ensureAdminsApproved();
  }
  return new Response(JSON.stringify({ has_admin: before, healed_admins: healed }), { status: 200 });
};
