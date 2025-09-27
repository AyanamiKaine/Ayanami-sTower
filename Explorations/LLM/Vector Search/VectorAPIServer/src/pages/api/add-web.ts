import type { APIRoute } from 'astro';
import { embedText } from '../../lib/embeddings';
import { insertDoc } from '../../lib/db';
import { generateSummary } from '../../lib/summarize';
import { generateTags } from '../../lib/tags';
import { countTokens } from '../../lib/tokens';

export const prerender = false;

async function fetchPage(url: string): Promise<string> {
  const res = await fetch(url, { headers: { 'User-Agent': 'VectorMemoryBot/1.0' } });
  if (!res.ok) throw new Error(`fetch failed status ${res.status}`);
  const html = await res.text();
  // Strip scripts/styles and collapse whitespace. A minimal readability-lite approach.
  let cleaned = html.replace(/<script[\s\S]*?<\/script>/gi, ' ') // remove scripts
                    .replace(/<style[\s\S]*?<\/style>/gi, ' ')   // remove styles
                    .replace(/<!--.*?-->/gs, ' ')                  // comments
                    .replace(/<head[\s\S]*?<\/head>/i, ' ')      // head
                    .replace(/<noscript[\s\S]*?<\/noscript>/gi,' ');
  // Keep alt text from images
  cleaned = cleaned.replace(/<img[^>]*alt="([^"]+)"[^>]*>/gi, ' $1 ');
  // Replace block elements with newlines
  cleaned = cleaned.replace(/<\/(p|div|section|article|h[1-6]|li|ul|ol|blockquote|pre|br)>/gi, '\n');
  // Strip remaining tags
  const text = cleaned.replace(/<[^>]+>/g, ' ').replace(/\n{3,}/g,'\n\n').replace(/\s{2,}/g,' ').trim();
  return text;
}

export const POST: APIRoute = async ({ request }) => {
  try {
    const body = await request.json();
    const url = body.url as string;
    if (!url || !/^https?:\/\//i.test(url)) return new Response(JSON.stringify({ error: 'valid http(s) url required'}), { status: 400 });
    const taskType = body.task_type as string | undefined;
    const autoSummarize = !!body.auto_summarize;
    const autoTag = !!body.auto_tag;
    const summaryTokens = body.summary_tokens ?? 80;
    const maxTags = body.max_tags ?? 6;

    let text = await fetchPage(url);
    if (!text) return new Response(JSON.stringify({ error: 'empty extracted text'}), { status: 422 });
    // Truncate extremely large pages (soft limit)
    const MAX_CHARS = 120_000; // ~ couple dozen pages
    if (text.length > MAX_CHARS) {
      text = text.slice(0, MAX_CHARS) + `\n[truncated length=${text.length}]`;
    }

  const embedding = await embedText(text, { isQuery: false, taskType: taskType });
  const tokenCount = await countTokens(text);
    let summary: string | undefined; let summaryEmbedding: Float32Array | null = null; let tags: string[] | undefined;
    if (autoSummarize) {
      try { summary = await generateSummary(text, { targetTokens: summaryTokens }); summaryEmbedding = await embedText(summary, { taskType, isQuery: false }); } catch {}
    }
    if (autoTag) {
      try { tags = await generateTags(text, { maxTags }); } catch {}
    }
  const id = insertDoc(text, embedding, undefined, summary, summaryEmbedding, tags, taskType, url, tokenCount ?? undefined);
  return new Response(JSON.stringify({ id, url, summary, tags, embedding_task: taskType, token_count: tokenCount }), { status: 201 });
  } catch (e: any) {
    return new Response(JSON.stringify({ error: e.message || 'failed'}), { status: 500 });
  }
};
