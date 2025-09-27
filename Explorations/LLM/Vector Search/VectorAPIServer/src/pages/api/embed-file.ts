import type { APIRoute } from 'astro';
import { embedText } from '../../lib/embeddings';
import { insertDoc } from '../../lib/db';
import { generateSummary } from '../../lib/summarize';
import { generateTags } from '../../lib/tags';

export const prerender = false;

// Accept multipart/form-data with fields:
//  file: uploaded file (.txt, .md, .pdf*)
//  task_type: optional embedding task type
//  auto_summarize (bool), summary_tokens
//  auto_tag (bool), max_tags
// NOTE: For PDF we currently do a naive text extraction if it's plain text; real PDF parsing would need an additional library.

export const POST: APIRoute = async ({ request }) => {
  const contentType = request.headers.get('content-type') || '';
  if (!contentType.includes('multipart/form-data')) {
    return new Response(JSON.stringify({ error: 'multipart/form-data required'}), { status: 400 });
  }
  const form = await request.formData();
  const file = form.get('file');
  if (!(file instanceof File)) return new Response(JSON.stringify({ error: 'file required'}), { status: 400 });
  const taskType = (form.get('task_type') as string) || undefined;
  const autoSummarize = form.get('auto_summarize') === 'true' || form.get('auto_summarize') === '1';
  const autoTag = form.get('auto_tag') === 'true' || form.get('auto_tag') === '1';
  const summaryTokens = form.get('summary_tokens') ? parseInt(form.get('summary_tokens') as string, 10) : 80;
  const maxTags = form.get('max_tags') ? parseInt(form.get('max_tags') as string, 10) : 6;

  let raw = await file.text();
  const filename = file.name.toLowerCase();
  if (filename.endsWith('.pdf')) {
    // Placeholder: PDF binary extraction not implemented
    return new Response(JSON.stringify({ error: 'PDF parsing not implemented yet' }), { status: 415 });
  }
  // Simple normalization for markdown: strip code fences optionally
  if (filename.endsWith('.md')) {
    raw = raw.replace(/```[\s\S]*?```/g, code => `\n[code block omitted length=${code.length}]\n`);
  }
  const text = raw.trim();
  if (!text) return new Response(JSON.stringify({ error: 'empty file'}), { status: 400 });

  const embedding = await embedText(text, { taskType, isQuery: false });
  let summary: string | undefined; let summaryEmbedding: Float32Array | null = null; let tags: string[] | undefined;
  if (autoSummarize) {
    try { summary = await generateSummary(text, { targetTokens: summaryTokens }); summaryEmbedding = await embedText(summary, { taskType, isQuery: false }); } catch {}
  }
  if (autoTag) {
    try { tags = await generateTags(text, { maxTags }); } catch {}
  }
  const id = insertDoc(text, embedding, undefined, summary, summaryEmbedding, tags, taskType);
  return new Response(JSON.stringify({ id, summary, tags, embedding_task: taskType }), { status: 201 });
};
