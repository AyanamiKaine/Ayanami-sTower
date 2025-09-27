// Use ESM import for better-sqlite3 so it works in Astro/Vite SSR (avoid require which is undefined)
// Types are provided via a loose ambient module declaration.
// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-ignore
import BetterSqlite3 from 'better-sqlite3';
import * as sqliteVec from 'sqlite-vec';
import bcrypt from 'bcryptjs';
import { DB_FILE } from './config';

let _db: any | null = null;

export function getDB(): any {
  if (_db) return _db;
  _db = new (BetterSqlite3 as any)(DB_FILE);
  // Load sqlite-vec functions
  sqliteVec.load(_db as any);
  initSchema();
  return _db;
}

function initSchema() {
  const db = _db!;
  db.exec(`CREATE TABLE IF NOT EXISTS docs (
    id INTEGER PRIMARY KEY,
    text TEXT NOT NULL,
    embedding BLOB NOT NULL,
    embedding_task TEXT,
    url TEXT,
    token_count INTEGER,
    summary TEXT,
    summary_embedding BLOB,
    tags TEXT,
    created_at INTEGER,
    updated_at INTEGER
  )`);
  // Users table (authentication)
  db.exec(`CREATE TABLE IF NOT EXISTS users (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    email TEXT UNIQUE NOT NULL,
    password_hash TEXT NOT NULL,
    is_admin INTEGER DEFAULT 0,
    is_approved INTEGER DEFAULT 0,
    created_at INTEGER NOT NULL,
    updated_at INTEGER NOT NULL,
    last_login INTEGER
  )`);
  // Sessions table
  db.exec(`CREATE TABLE IF NOT EXISTS sessions (
    token TEXT PRIMARY KEY,
    user_id INTEGER NOT NULL,
    created_at INTEGER NOT NULL,
    expires_at INTEGER NOT NULL,
    FOREIGN KEY(user_id) REFERENCES users(id) ON DELETE CASCADE
  )`);
  // Add summary column if migrating older schema
  const columns: { name: string }[] = db.prepare("PRAGMA table_info(docs)").all().map((r:any)=>({name:r.name}));
  const colNames = new Set(columns.map(c=>c.name));
  if (!colNames.has('summary')) {
    db.exec('ALTER TABLE docs ADD COLUMN summary TEXT');
  }
  if (!colNames.has('summary_embedding')) {
    db.exec('ALTER TABLE docs ADD COLUMN summary_embedding BLOB');
  }
  if (!colNames.has('tags')) {
    db.exec('ALTER TABLE docs ADD COLUMN tags TEXT');
  }
  if (!colNames.has('embedding_task')) {
    db.exec('ALTER TABLE docs ADD COLUMN embedding_task TEXT');
  }
  if (!colNames.has('url')) {
    db.exec('ALTER TABLE docs ADD COLUMN url TEXT');
  }
  if (!colNames.has('token_count')) {
    db.exec('ALTER TABLE docs ADD COLUMN token_count INTEGER');
  }
  // Backfill timestamps
  const now = Math.floor(Date.now() / 1000);
  db.exec(`UPDATE docs SET created_at = COALESCE(created_at, ${now}), updated_at = COALESCE(updated_at, ${now})`);
}

export interface StoredDoc { id: number; text: string; embedding: ArrayBuffer; embedding_task?: string | null; url?: string | null; token_count?: number | null; summary?: string | null; summary_embedding?: ArrayBuffer | null; tags?: string[] | null; created_at: number; updated_at: number; }

export function insertDoc(text: string, embedding: Float32Array, id?: number, summary?: string, summaryEmbedding?: Float32Array | null, tags?: string[] | null, embeddingTask?: string, url?: string, tokenCount?: number): number {
  const db = getDB();
  const ts = Math.floor(Date.now() / 1000);
  const buf = Buffer.from(new Uint8Array(embedding.buffer.slice(0)));
  const sumBuf = summaryEmbedding ? Buffer.from(new Uint8Array(summaryEmbedding.buffer.slice(0))) : null;
  const tagsJson = tags && tags.length ? JSON.stringify(tags) : null;
  if (id != null) {
    const existing = db.prepare('SELECT id FROM docs WHERE id=?').get(id) as any;
    if (existing) throw new Error('Document with that ID already exists');
  }
  const stmt = db.prepare('INSERT INTO docs(id,text,embedding,embedding_task,url,token_count,summary,summary_embedding,tags,created_at,updated_at) VALUES(?,?,?,?,?,?,?,?,?,?,?)');
  const newId = id ?? ((db.prepare('SELECT COALESCE(MAX(id),0)+1 AS nid FROM docs').get() as any).nid);
  stmt.run(newId, text, buf, embeddingTask ?? null, url ?? null, tokenCount ?? null, summary ?? null, sumBuf, tagsJson, ts, ts);
  return newId;
}

export function updateDoc(id: number, text: string, embedding: Float32Array, embeddingTask?: string, tokenCount?: number) {
  const db = getDB();
  const ts = Math.floor(Date.now() / 1000);
  const buf = Buffer.from(new Uint8Array(embedding.buffer.slice(0)));
  const res = db.prepare('UPDATE docs SET text=?, embedding=?, embedding_task=COALESCE(?, embedding_task), token_count=COALESCE(?, token_count), updated_at=? WHERE id=?').run(text, buf, embeddingTask ?? null, tokenCount ?? null, ts, id);
  if (res.changes === 0) throw new Error('Not found');
}

export function updateDocSummary(id: number, summary: string, summaryEmbedding?: Float32Array) {
  const db = getDB();
  const ts = Math.floor(Date.now() / 1000);
  const sumBuf = summaryEmbedding ? Buffer.from(new Uint8Array(summaryEmbedding.buffer.slice(0))) : null;
  const res = db.prepare('UPDATE docs SET summary=?, summary_embedding=COALESCE(?, summary_embedding), updated_at=? WHERE id=?').run(summary, sumBuf, ts, id);
  if (res.changes === 0) throw new Error('Not found');
}

export function updateDocTags(id: number, tags: string[]) {
  const db = getDB();
  const ts = Math.floor(Date.now() / 1000);
  const tagsJson = tags && tags.length ? JSON.stringify(tags) : null;
  const res = db.prepare('UPDATE docs SET tags=?, updated_at=? WHERE id=?').run(tagsJson, ts, id);
  if (res.changes === 0) throw new Error('Not found');
}

export function getDoc(id: number): StoredDoc | null {
  const db = getDB();
  const row = db.prepare('SELECT id, text, embedding, embedding_task, url, token_count, summary, summary_embedding, tags, created_at, updated_at FROM docs WHERE id=?').get(id) as any;
  if (!row) return null;
  if (row.tags) {
    try { row.tags = JSON.parse(row.tags); } catch { row.tags = null; }
  }
  return row as StoredDoc;
}

export function deleteDoc(id: number) {
  const db = getDB();
  const res = db.prepare('DELETE FROM docs WHERE id=?').run(id);
  if (res.changes === 0) throw new Error('Not found');
}

export function countDocs(): number {
  const db = getDB();
  const row = db.prepare('SELECT COUNT(*) AS c FROM docs').get() as any;
  return row.c as number;
}

export interface ListedDoc { id:number; text:string; summary?:string|null; tags?:string[]|null; embedding_task?: string | null; url?: string | null; token_count?: number | null; created_at:number; updated_at:number; }

export function listDocs(opts: { limit?: number; offset?: number; order?: 'recent'|'id' } = {}): ListedDoc[] {
  const db = getDB();
  const limit = Math.min(Math.max(opts.limit ?? 200, 1), 1000);
  const offset = Math.max(opts.offset ?? 0, 0);
  const order = opts.order === 'recent' ? 'updated_at DESC' : 'id ASC';
  const rows = db.prepare(`SELECT id, text, summary, tags, embedding_task, url, token_count, created_at, updated_at FROM docs ORDER BY ${order} LIMIT ? OFFSET ?`).all(limit, offset) as any[];
  rows.forEach(r => { if (r.tags) { try { r.tags = JSON.parse(r.tags); } catch { r.tags = null; } } });
  return rows as ListedDoc[];
}

export interface Retrieved { id: number; text: string; summary?: string | null; tags?: string[] | null; embedding_task?: string | null; url?: string | null; token_count?: number | null; distance: number; created_at: number; updated_at: number; }

export function search(embedding: Float32Array, topK: number, useRecency: boolean, halfLife: number, alpha: number, opts?: { useSummary?: boolean; fallbackFull?: boolean }): Retrieved[] {
  const db = getDB();
  const buffer = Buffer.from(new Uint8Array(embedding.buffer));
  const limit = useRecency ? Math.max(topK * 5, 50) : topK;
  const useSummary = opts?.useSummary;
  // Choose embedding column based on useSummary flag. If summary embedding missing and fallback allowed, fall back per-row.
  if (!useSummary) {
  const stmt = db.prepare('SELECT id, text, summary, tags, embedding_task, url, token_count, vec_distance_l2(embedding, ?) AS distance, created_at, updated_at FROM docs ORDER BY distance ASC LIMIT ?');
    const rows = stmt.all(buffer, limit) as any[];
    rows.forEach(r => { if (r.tags) { try { r.tags = JSON.parse(r.tags); } catch { r.tags = null; } } });
  if (!useRecency) return rows.map(r => ({ id: r.id, text: r.text, summary: r.summary, tags: r.tags, embedding_task: r.embedding_task, url: r.url, token_count: r.token_count, distance: r.distance, created_at: r.created_at, updated_at: r.updated_at }));
    const now = Math.floor(Date.now() / 1000);
    const rescored = rows.map(r => {
      const upd = r.updated_at || r.created_at || now;
      const age = Math.max(0, now - upd);
      const recencyFactor = Math.exp(-age / halfLife);
      const adjusted = r.distance - alpha * recencyFactor;
      return { adjusted, r };
  }).sort((a,b) => a.adjusted - b.adjusted).slice(0, topK).map(x => ({ id: x.r.id, text: x.r.text, summary: x.r.summary, tags: x.r.tags, embedding_task: x.r.embedding_task, url: x.r.url, token_count: x.r.token_count, distance: x.r.distance, created_at: x.r.created_at, updated_at: x.r.updated_at }));
    return rescored;
  }
  // Summary search path
  const rows = db.prepare('SELECT id, text, summary, tags, embedding_task, url, token_count, summary_embedding, embedding, created_at, updated_at FROM docs WHERE summary_embedding IS NOT NULL').all() as any[];
  // If no summaries present, optionally fall back
  if (rows.length === 0 && opts?.fallbackFull) {
    return search(embedding, topK, useRecency, halfLife, alpha, { useSummary: false });
  }
  // Compute distances manually using JS for flexibility
  const q = new Float32Array(embedding.buffer.slice(0));
  const distances = rows.map(r => {
    if (!r.summary_embedding) return { r, d: Number.POSITIVE_INFINITY };
    const buf: Buffer = r.summary_embedding as Buffer;
    const arr = new Float32Array(buf.buffer, buf.byteOffset, buf.byteLength/4);
    let sum = 0;
    const len = Math.min(q.length, arr.length);
    for (let i=0;i<len;i++) {
      const diff = arr[i] - q[i];
      sum += diff*diff;
    }
    return { r, d: Math.sqrt(sum) };
  }).sort((a,b)=> a.d - b.d).slice(0, useRecency ? Math.max(topK*5,50): topK);
  distances.forEach(x => { if (x.r.tags) { try { x.r.tags = JSON.parse(x.r.tags); } catch { x.r.tags = null; } } });
  if (!useRecency) {
  return distances.slice(0, topK).map(x => ({ id: x.r.id, text: x.r.text, summary: x.r.summary, tags: x.r.tags, embedding_task: x.r.embedding_task, url: x.r.url, token_count: x.r.token_count, distance: x.d, created_at: x.r.created_at, updated_at: x.r.updated_at }));
  }
  const now = Math.floor(Date.now()/1000);
  const rescored = distances.map(x => {
    const upd = x.r.updated_at || x.r.created_at || now;
    const age = Math.max(0, now - upd);
    const recencyFactor = Math.exp(-age / halfLife);
    const adjusted = x.d - alpha * recencyFactor;
    return { adjusted, r: x.r, original: x.d };
  }).sort((a,b)=> a.adjusted - b.adjusted).slice(0, topK).map(x => ({ id: x.r.id, text: x.r.text, summary: x.r.summary, tags: x.r.tags, embedding_task: x.r.embedding_task, url: x.r.url, token_count: x.r.token_count, distance: x.original, created_at: x.r.created_at, updated_at: x.r.updated_at }));
  return rescored;
}

// ================= AUTH HELPERS =================
export interface User { id:number; email:string; password_hash:string; is_admin:number; is_approved:number; created_at:number; updated_at:number; last_login?:number|null }

export function getUserByEmail(email: string): User | null {
  const db = getDB();
  const row = db.prepare('SELECT * FROM users WHERE email=?').get(email) as any;
  return row || null;
}

export function getUserById(id: number): User | null {
  const db = getDB();
  const row = db.prepare('SELECT * FROM users WHERE id=?').get(id) as any;
  return row || null;
}

export function createUser(email: string, passwordHash: string, { approved = false, admin = false } = {}): number {
  const db = getDB();
  const ts = Math.floor(Date.now()/1000);
  const stmt = db.prepare('INSERT INTO users(email,password_hash,is_admin,is_approved,created_at,updated_at) VALUES(?,?,?,?,?,?)');
  const info = stmt.run(email, passwordHash, admin ? 1 : 0, approved ? 1 : 0, ts, ts);
  return info.lastInsertRowid as number;
}

export function approveUser(id: number) {
  const db = getDB();
  const ts = Math.floor(Date.now()/1000);
  db.prepare('UPDATE users SET is_approved=1, updated_at=? WHERE id=?').run(ts, id);
}

export function listUsers(): User[] {
  const db = getDB();
  return db.prepare('SELECT id,email,is_admin,is_approved,created_at,updated_at,last_login,password_hash FROM users ORDER BY id ASC').all() as any[];
}

export function deleteUser(id: number) {
  const db = getDB();
  db.prepare('DELETE FROM users WHERE id=?').run(id);
}

export function hasAdmin(): boolean {
  const db = getDB();
  const row = db.prepare('SELECT COUNT(*) as c FROM users WHERE is_admin=1').get() as any;
  return (row?.c || 0) > 0;
}

// Ensure any admin accounts are approved (healing function if state got inconsistent)
export function ensureAdminsApproved(): number {
  const db = getDB();
  const ts = Math.floor(Date.now()/1000);
  const info = db.prepare('UPDATE users SET is_approved=1, updated_at=? WHERE is_admin=1 AND is_approved=0').run(ts);
  return info.changes as number;
}

export function updateUserPasswordHash(id: number, hash: string) {
  const db = getDB();
  const ts = Math.floor(Date.now()/1000);
  db.prepare('UPDATE users SET password_hash=?, updated_at=? WHERE id=?').run(hash, ts, id);
}

export interface Session { token:string; user_id:number; created_at:number; expires_at:number }

export function createSession(userId: number, ttlSeconds = 7*24*3600): string {
  const db = getDB();
  const ts = Math.floor(Date.now()/1000);
  const token = crypto.randomUUID();
  const expires = ts + ttlSeconds;
  db.prepare('INSERT INTO sessions(token,user_id,created_at,expires_at) VALUES(?,?,?,?)').run(token, userId, ts, expires);
  db.prepare('UPDATE users SET last_login=?, updated_at=? WHERE id=?').run(ts, ts, userId);
  return token;
}

export function getSession(token: string): Session | null {
  const db = getDB();
  const row = db.prepare('SELECT token,user_id,created_at,expires_at FROM sessions WHERE token=?').get(token) as any;
  if (!row) return null;
  const now = Math.floor(Date.now()/1000);
  if (row.expires_at < now) {
    try { db.prepare('DELETE FROM sessions WHERE token=?').run(token); } catch {}
    return null;
  }
  return row as Session;
}

export function deleteSession(token: string) {
  const db = getDB();
  db.prepare('DELETE FROM sessions WHERE token=?').run(token);
}

export function ensureAdminUser() {
  // Create default admin if none exists
  const db = getDB();
  const admin = db.prepare('SELECT id FROM users WHERE is_admin=1').get() as any;
  if (!admin) {
    // Generate random password and hash
    const pw = (Math.random().toString(36).slice(2,10) + Math.random().toString(36).slice(2,6));
    const hash = bcrypt.hashSync(pw, 10);
    const ts = Math.floor(Date.now()/1000);
    db.prepare('INSERT INTO users(email,password_hash,is_admin,is_approved,created_at,updated_at) VALUES(?,?,?,?,?,?)')
      .run('admin@example.com', hash, 1, 1, ts, ts);
    console.log('\n[auth] Created default admin user: admin@example.com  password:', pw, '\nStore this securely and change after first login.');
  }
}

