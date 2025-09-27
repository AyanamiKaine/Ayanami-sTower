import { GEMINI_API_KEY, GEMINI_GENERATION_MODEL } from './config';
import { GoogleGenAI } from '@google/genai';

const MODE_INSTRUCTIONS: Record<string,string> = {
  summarize: 'Produce a compressed but information-rich paraphrase suitable for semantic vector search.',
  expand: 'Rewrite the question with explicit entities, context, and missing nouns so it is self-contained.',
  disambiguate: 'Rewrite to remove pronouns and ambiguous references; specify concrete entities.',
  auto: 'Rewrite to be self-contained and explicit for semantic vector search.'
};

function shouldAutoExpand(q: string): boolean {
  const short = q.trim().length < 20;
  const pronouns = new Set(['it','they','them','this','that','those']);
  const tokens = q.split(/\s+/).map(t=>t.toLowerCase().replace(/[.,!?]/g,''));
  const pronouny = tokens.some(t=>pronouns.has(t));
  const vague = ['and then','then what','what next','how about'].some(p=>q.toLowerCase().startsWith(p));
  return short || pronouny || vague;
}

export async function rewriteQuery(original: string, mode: string): Promise<string> {
  if (!mode || mode === 'none') return original;
  let applied = mode;
  if (mode === 'auto' && !shouldAutoExpand(original)) return original;
  if (mode === 'auto') applied = 'expand';
  if (!GEMINI_API_KEY) return original; // no key -> skip
  try {
    const client = new GoogleGenAI({ apiKey: GEMINI_API_KEY });
    const instruction = MODE_INSTRUCTIONS[applied] || MODE_INSTRUCTIONS.expand;
    const prompt = `You rewrite user queries for vector similarity search. Output only the rewritten query.\nRewrite style: ${instruction}\nUser: ${original}\nRewritten:`;
    const resp = await client.models.generateContent({ model: GEMINI_GENERATION_MODEL, contents: prompt });
    const text: string = (resp?.text || '').split(/\n/)[0].trim();
    if (text) {
      const unq = text.replace(/^['\"]|['\"]$/g,'').trim();
      return unq || original;
    }
    return original;
  } catch (e) {
    return original;
  }
}
