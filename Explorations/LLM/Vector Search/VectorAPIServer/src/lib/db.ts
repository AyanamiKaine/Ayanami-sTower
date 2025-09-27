// Use ESM import for better-sqlite3 so it works in Astro/Vite SSR (avoid require which is undefined)
// Types are provided via a loose ambient module declaration.
// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-ignore
import BetterSqlite3 from 'better-sqlite3';
import * as sqliteVec from 'sqlite-vec';
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
    summary TEXT,
    summary_embedding BLOB,
    tags TEXT,
    created_at INTEGER,
    updated_at INTEGER
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
  // Backfill timestamps
  const now = Math.floor(Date.now() / 1000);
  db.exec(`UPDATE docs SET created_at = COALESCE(created_at, ${now}), updated_at = COALESCE(updated_at, ${now})`);
}

export interface StoredDoc { id: number; text: string; embedding: ArrayBuffer; embedding_task?: string | null; url?: string | null; summary?: string | null; summary_embedding?: ArrayBuffer | null; tags?: string[] | null; created_at: number; updated_at: number; }

export function insertDoc(text: string, embedding: Float32Array, id?: number, summary?: string, summaryEmbedding?: Float32Array | null, tags?: string[] | null, embeddingTask?: string, url?: string): number {
  const db = getDB();
  const ts = Math.floor(Date.now() / 1000);
  const buf = Buffer.from(new Uint8Array(embedding.buffer.slice(0)));
  const sumBuf = summaryEmbedding ? Buffer.from(new Uint8Array(summaryEmbedding.buffer.slice(0))) : null;
  const tagsJson = tags && tags.length ? JSON.stringify(tags) : null;
  if (id != null) {
    const existing = db.prepare('SELECT id FROM docs WHERE id=?').get(id) as any;
    if (existing) throw new Error('Document with that ID already exists');
  }
  const stmt = db.prepare('INSERT INTO docs(id,text,embedding,embedding_task,url,summary,summary_embedding,tags,created_at,updated_at) VALUES(?,?,?,?,?,?,?,?,?,?)');
  const newId = id ?? ((db.prepare('SELECT COALESCE(MAX(id),0)+1 AS nid FROM docs').get() as any).nid);
  stmt.run(newId, text, buf, embeddingTask ?? null, url ?? null, summary ?? null, sumBuf, tagsJson, ts, ts);
  return newId;
}

export function updateDoc(id: number, text: string, embedding: Float32Array, embeddingTask?: string) {
  const db = getDB();
  const ts = Math.floor(Date.now() / 1000);
  const buf = Buffer.from(new Uint8Array(embedding.buffer.slice(0)));
  const res = db.prepare('UPDATE docs SET text=?, embedding=?, embedding_task=COALESCE(?, embedding_task), updated_at=? WHERE id=?').run(text, buf, embeddingTask ?? null, ts, id);
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
  const row = db.prepare('SELECT id, text, embedding, embedding_task, url, summary, summary_embedding, tags, created_at, updated_at FROM docs WHERE id=?').get(id) as any;
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

export interface ListedDoc { id:number; text:string; summary?:string|null; tags?:string[]|null; embedding_task?: string | null; url?: string | null; created_at:number; updated_at:number; }

export function listDocs(opts: { limit?: number; offset?: number; order?: 'recent'|'id' } = {}): ListedDoc[] {
  const db = getDB();
  const limit = Math.min(Math.max(opts.limit ?? 200, 1), 1000);
  const offset = Math.max(opts.offset ?? 0, 0);
  const order = opts.order === 'recent' ? 'updated_at DESC' : 'id ASC';
  const rows = db.prepare(`SELECT id, text, summary, tags, embedding_task, url, created_at, updated_at FROM docs ORDER BY ${order} LIMIT ? OFFSET ?`).all(limit, offset) as any[];
  rows.forEach(r => { if (r.tags) { try { r.tags = JSON.parse(r.tags); } catch { r.tags = null; } } });
  return rows as ListedDoc[];
}

export interface Retrieved { id: number; text: string; summary?: string | null; tags?: string[] | null; embedding_task?: string | null; url?: string | null; distance: number; created_at: number; updated_at: number; }

export function search(embedding: Float32Array, topK: number, useRecency: boolean, halfLife: number, alpha: number, opts?: { useSummary?: boolean; fallbackFull?: boolean }): Retrieved[] {
  const db = getDB();
  const buffer = Buffer.from(new Uint8Array(embedding.buffer));
  const limit = useRecency ? Math.max(topK * 5, 50) : topK;
  const useSummary = opts?.useSummary;
  // Choose embedding column based on useSummary flag. If summary embedding missing and fallback allowed, fall back per-row.
  if (!useSummary) {
  const stmt = db.prepare('SELECT id, text, summary, tags, embedding_task, url, vec_distance_l2(embedding, ?) AS distance, created_at, updated_at FROM docs ORDER BY distance ASC LIMIT ?');
    const rows = stmt.all(buffer, limit) as any[];
    rows.forEach(r => { if (r.tags) { try { r.tags = JSON.parse(r.tags); } catch { r.tags = null; } } });
  if (!useRecency) return rows.map(r => ({ id: r.id, text: r.text, summary: r.summary, tags: r.tags, embedding_task: r.embedding_task, url: r.url, distance: r.distance, created_at: r.created_at, updated_at: r.updated_at }));
    const now = Math.floor(Date.now() / 1000);
    const rescored = rows.map(r => {
      const upd = r.updated_at || r.created_at || now;
      const age = Math.max(0, now - upd);
      const recencyFactor = Math.exp(-age / halfLife);
      const adjusted = r.distance - alpha * recencyFactor;
      return { adjusted, r };
  }).sort((a,b) => a.adjusted - b.adjusted).slice(0, topK).map(x => ({ id: x.r.id, text: x.r.text, summary: x.r.summary, tags: x.r.tags, embedding_task: x.r.embedding_task, url: x.r.url, distance: x.r.distance, created_at: x.r.created_at, updated_at: x.r.updated_at }));
    return rescored;
  }
  // Summary search path
  const rows = db.prepare('SELECT id, text, summary, tags, embedding_task, url, summary_embedding, embedding, created_at, updated_at FROM docs WHERE summary_embedding IS NOT NULL').all() as any[];
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
  return distances.slice(0, topK).map(x => ({ id: x.r.id, text: x.r.text, summary: x.r.summary, tags: x.r.tags, embedding_task: x.r.embedding_task, url: x.r.url, distance: x.d, created_at: x.r.created_at, updated_at: x.r.updated_at }));
  }
  const now = Math.floor(Date.now()/1000);
  const rescored = distances.map(x => {
    const upd = x.r.updated_at || x.r.created_at || now;
    const age = Math.max(0, now - upd);
    const recencyFactor = Math.exp(-age / halfLife);
    const adjusted = x.d - alpha * recencyFactor;
    return { adjusted, r: x.r, original: x.d };
  }).sort((a,b)=> a.adjusted - b.adjusted).slice(0, topK).map(x => ({ id: x.r.id, text: x.r.text, summary: x.r.summary, tags: x.r.tags, embedding_task: x.r.embedding_task, url: x.r.url, distance: x.original, created_at: x.r.created_at, updated_at: x.r.updated_at }));
  return rescored;
}
