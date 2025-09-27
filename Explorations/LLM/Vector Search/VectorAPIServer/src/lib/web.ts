// Minimal HTML -> text extraction used for web page ingestion/refetch.
// Not a full readability implementation; focuses on removing noise while keeping core text.
export async function fetchPageText(url: string): Promise<string> {
  const res = await fetch(url, { headers: { 'User-Agent': 'VectorMemoryBot/1.0' } });
  if (!res.ok) throw new Error(`fetch failed status ${res.status}`);
  const html = await res.text();
  let cleaned = html
    .replace(/<script[\s\S]*?<\/script>/gi, ' ')
    .replace(/<style[\s\S]*?<\/style>/gi, ' ')
    .replace(/<!--.*?-->/gs, ' ')
    .replace(/<head[\s\S]*?<\/head>/i, ' ')
    .replace(/<noscript[\s\S]*?<\/noscript>/gi, ' ');
  cleaned = cleaned.replace(/<img[^>]*alt="([^"]+)"[^>]*>/gi, ' $1 ');
  cleaned = cleaned.replace(/<\/(p|div|section|article|h[1-6]|li|ul|ol|blockquote|pre|br)>/gi, '\n');
  const text = cleaned.replace(/<[^>]+>/g, ' ').replace(/\n{3,}/g,'\n\n').replace(/\s{2,}/g,' ').trim();
  return text;
}
