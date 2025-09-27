import { getSession, getUserById, createUser, getUserByEmail, createSession, deleteSession, approveUser, listUsers, deleteUser, hasAdmin, updateUserPasswordHash } from './db';
import type { APIRoute } from 'astro';
import bcrypt from 'bcryptjs';

// Removed automatic console-based admin creation. First registered user becomes admin & approved.

export async function hashPassword(pw: string) {
  const salt = await bcrypt.genSalt(10);
  return bcrypt.hash(pw, salt);
}

export async function verifyPassword(pw: string, hash: string) {
  return bcrypt.compare(pw, hash);
}

export interface AuthContext { userId: number; email: string; isAdmin: boolean; }

export function extractToken(req: Request): string | null {
  const auth = req.headers.get('authorization');
  if (auth && auth.startsWith('Bearer ')) return auth.slice(7).trim();
  const cookie = req.headers.get('cookie');
  if (cookie) {
    const m = /session=([^;]+)/.exec(cookie);
    if (m) return decodeURIComponent(m[1]);
  }
  return null;
}

export function getAuthUser(req: Request): AuthContext | null {
  const token = extractToken(req);
  if (!token) return null;
  const session = getSession(token);
  if (!session) return null;
  const user = getUserById(session.user_id);
  if (!user) return null;
  if (!user.is_approved) return null;
  return { userId: user.id, email: user.email, isAdmin: !!user.is_admin };
}

export function requireAuth(req: Request): AuthContext | Response {
  const ctx = getAuthUser(req);
  if (!ctx) return new Response(JSON.stringify({ error: 'unauthorized'}), { status: 401 });
  return ctx;
}

export function setSessionCookie(token: string, maxAgeSeconds = 7*24*3600) {
  return `session=${encodeURIComponent(token)}; HttpOnly; Path=/; Max-Age=${maxAgeSeconds}; SameSite=Lax`;
}

export async function registerUser(email: string, password: string) {
  const existing = getUserByEmail(email);
  if (existing) throw new Error('email already registered');
  const hash = await hashPassword(password);
  const first = !hasAdmin();
  const id = createUser(email, hash, { approved: first, admin: first });
  return id;
}

export async function loginUser(email: string, password: string) {
  const user = getUserByEmail(email);
  if (!user) throw new Error('invalid credentials');
  const ok = await verifyPassword(password, user.password_hash);
  if (!ok) throw new Error('invalid credentials');
  if (!user.is_approved) throw new Error('not approved');
  const token = createSession(user.id);
  return { token, user };
}

export function logoutUser(token: string) { deleteSession(token); }

export function adminApproveUser(id: number) { approveUser(id); }

export function adminListUsers() { return listUsers().map(u => ({ id: u.id, email: u.email, is_admin: !!u.is_admin, is_approved: !!u.is_approved, created_at: u.created_at, updated_at: u.updated_at, last_login: u.last_login })); }

export function adminDeleteUser(id: number) { deleteUser(id); }

export async function changePassword(userId: number, newPassword: string) {
  const hash = await hashPassword(newPassword);
  updateUserPasswordHash(userId, hash);
}
