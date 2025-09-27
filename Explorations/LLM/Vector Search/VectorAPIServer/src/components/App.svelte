<script lang="ts">
  import { onMount } from 'svelte';
  type Result = { id:number; text:string; summary?:string; tags?:string[]; distance:number; created_at?:number; updated_at?:number };
  let query = '';
  let results: Result[] = [];
  let loading = false;
  let top_k = 5;
  let rewrite_mode = 'none';
  let rewritten_query = '';
  let use_recency = false;
  let recency_half_life = 7*24*3600;
  let recency_alpha = 0.3;
  let use_summary = false;
  let newDocText = '';
  let addLoading = false;
  let autoSummarize = true;
  let summaryTokens = 80;
  let autoTag = true;
  let maxTags = 6;
  let retagLoading: Record<number, boolean> = {};
  let resummarizeLoading: Record<number, boolean> = {};
  let stats: any = null;
  // Listing state
  interface ListedDoc { id:number; text:string; summary?:string|null; tags?:string[]|null; created_at:number; updated_at:number }
  let allDocs: ListedDoc[] = [];
  let listLoading = false;
  let listOrder: 'id' | 'recent' = 'recent';
  let listLimit = 100;
  let listOffset = 0;
  let deleting: Record<number, boolean> = {};
  let error: string | null = null;

  async function fetchStats() {
    try { const r = await fetch('/api/stats'); stats = await r.json(); } catch {}
  }

  function formatTs(ts?:number) {
    if (!ts) return '';
    const d = new Date(ts*1000); return d.toISOString().split('T')[0];
  }

  async function runSearch() {
    if (!query.trim()) return;
    loading = true; error = null;
    try {
  const body = { query, top_k, rewrite_mode, use_recency, recency_half_life, recency_alpha, use_summary };
      const r = await fetch('/api/search', { method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify(body)});
      const data = await r.json();
      if (!r.ok) throw new Error(data.error || 'search failed');
      results = data.results; rewritten_query = data.rewritten_query;
    } catch (e:any) {
      error = e.message;
    } finally { loading = false; }
  }

  async function addDoc() {
    if (!newDocText.trim()) return;
    addLoading = true; error = null;
    try {
  const r = await fetch('/api/documents', { method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify({ text: newDocText, auto_summarize: autoSummarize, summary_tokens: summaryTokens, auto_tag: autoTag, max_tags: maxTags })});
      const data = await r.json();
      if (!r.ok) throw new Error(data.error || 'add failed');
      newDocText='';
      fetchStats();
    } catch(e:any) { error = e.message; } finally { addLoading=false; }
  }

  async function triggerResummarize(id: number) {
    resummarizeLoading[id] = true;
    try {
      const r = await fetch(`/api/documents?id=${id}`, { method:'PATCH', headers:{'Content-Type':'application/json'}, body: JSON.stringify({ summary_tokens: summaryTokens })});
      const data = await r.json();
      if (r.ok) {
        // refresh results in place
        results = results.map(x => x.id === id ? { ...x, summary: data.summary } : x);
      }
    } catch {} finally {
      resummarizeLoading[id] = false;
    }
  }

  async function triggerRetag(id: number) {
    retagLoading[id] = true;
    try {
      const r = await fetch(`/api/documents?id=${id}`, { method:'PATCH', headers:{'Content-Type':'application/json'}, body: JSON.stringify({ action: 'tag', max_tags: maxTags })});
      const data = await r.json();
      if (r.ok) {
        results = results.map(x => x.id === id ? { ...x, tags: data.tags } : x);
      }
    } catch {} finally {
      retagLoading[id] = false;
    }
  }

  onMount(fetchStats);

  async function loadDocs() {
    listLoading = true; error = null;
    try {
      const params = new URLSearchParams({ limit: String(listLimit), offset: String(listOffset), order: listOrder });
      const r = await fetch(`/api/list?${params.toString()}`);
      const data = await r.json();
      if (!r.ok) throw new Error(data.error || 'list failed');
      allDocs = data.docs;
    } catch(e:any) { error = e.message; } finally { listLoading = false; }
  }

  async function deleteDoc(id: number) {
    if (!confirm(`Delete document ${id}?`)) return;
    deleting[id] = true;
    try {
      const r = await fetch(`/api/documents?id=${id}`, { method: 'DELETE' });
      if (r.status === 204) {
        allDocs = allDocs.filter(d => d.id !== id);
        results = results.filter(r => r.id !== id);
        fetchStats();
      }
    } catch {} finally { deleting[id] = false; }
  }
</script>

<style>
  /* Legacy styles removed in favor of Tailwind utility classes */
</style>

<div class="max-w-[1500px] mx-auto px-6 py-6 flex flex-col gap-6">
  <header class="flex flex-col gap-2">
    <h1 class="text-2xl font-bold tracking-tight flex items-center gap-2">Vector Memory Console <span class="text-xs font-medium bg-brand-100 text-brand-700 px-2 py-0.5 rounded">Gemini</span></h1>
    <p class="text-sm text-slate-600">Search, summarize, tag and manage your local embedding store.</p>
  </header>

  <div class="grid gap-6 xl:grid-cols-3">
    <!-- Search Panel -->
    <div class="panel xl:col-span-2">
      <div class="flex items-end gap-3 flex-wrap">
        <div class="flex-1 min-w-[240px]">
          <label for="q" class="text-xs font-medium text-slate-600 uppercase tracking-wide">Query</label>
          <input id="q" class="input w-full mt-1" placeholder="Semantic / natural language query..." bind:value={query} on:keydown={(e)=> e.key==='Enter' && runSearch()} />
        </div>
        <div class="flex items-center gap-3">
          <button class="btn" disabled={loading} on:click={runSearch}>{loading ? 'Searching...' : 'Search'}</button>
        </div>
      </div>
      <div class="flex flex-wrap gap-4 text-xs mt-4 items-end">
        <label class="flex items-center gap-2">Top K <input class="input w-20" type="number" min="1" max="50" bind:value={top_k}/></label>
        <label class="flex items-center gap-2">Rewrite
          <select class="input" bind:value={rewrite_mode}>
            <option value="none">none</option>
            <option value="summarize">summarize</option>
            <option value="expand">expand</option>
            <option value="disambiguate">disambiguate</option>
            <option value="auto">auto</option>
          </select>
        </label>
        <label class="flex items-center gap-2"><input class="checkbox" type="checkbox" bind:checked={use_recency}/> <span>recency</span></label>
        <label class="flex items-center gap-2"><input class="checkbox" type="checkbox" bind:checked={use_summary}/> <span>summary search</span></label>
        {#if use_recency}
          <label class="flex items-center gap-2">half-life <input class="input w-28" type="number" bind:value={recency_half_life} /></label>
          <label class="flex items-center gap-2">alpha <input class="input w-20" type="number" step="0.05" min="0" max="2" bind:value={recency_alpha} /></label>
        {/if}
      </div>
      {#if rewritten_query && rewritten_query !== query}
        <div class="mt-3 text-xs text-slate-600">Rewritten: <code class="px-1 py-0.5 rounded bg-slate-100 text-slate-700">{rewritten_query}</code></div>
      {/if}
      {#if error}<div class="mt-3 text-rose-600 text-sm">{error}</div>{/if}
      <div class="mt-6 flex items-center justify-between">
        <h3 class="text-sm font-semibold tracking-wide text-slate-700 uppercase">Results</h3>
        <div class="text-xs text-slate-500">{results.length} shown</div>
      </div>
      {#if results.length === 0}
        <div class="text-slate-400 text-sm mt-2">No results yet.</div>
      {:else}
      <div class="overflow-auto max-h-[420px] border border-slate-200 rounded-lg">
        <table class="data">
          <thead>
            <tr>
              <th class="px-3 py-2">ID</th>
              <th class="px-3 py-2">Dist</th>
              <th class="px-3 py-2">Updated</th>
              <th class="px-3 py-2 w-[30%]">Text</th>
              <th class="px-3 py-2 w-[28%]">Summary</th>
              <th class="px-3 py-2 w-[14%]">Tags</th>
              <th class="px-3 py-2"></th>
            </tr>
          </thead>
          <tbody>
            {#each results as r}
              <tr class="hover:bg-slate-50 transition">
                <td class="px-3 py-2"><span class="badge">{r.id}</span></td>
                <td class="px-3 py-2 text-[11px] tabular-nums">{r.distance.toFixed(4)}</td>
                <td class="px-3 py-2 text-[11px]">{formatTs(r.updated_at) || formatTs(r.created_at)}</td>
                <td class="px-3 py-2 text-xs whitespace-pre-wrap">{r.text}</td>
                <td class="px-3 py-2 text-[11px] whitespace-pre-wrap">{r.summary || '—'}</td>
                <td class="px-3 py-2">
                  {#if r.tags}
                    <div class="flex flex-wrap">{#each r.tags as tg}<span class="tag-badge">{tg}</span>{/each}</div>
                  {:else} <span class="text-slate-400 text-[11px]">—</span>{/if}
                </td>
                <td class="px-3 py-2">
                  <div class="flex flex-col gap-1">
                    <button class="btn-secondary btn !text-xs" disabled={resummarizeLoading[r.id]} on:click={()=> triggerResummarize(r.id)}>{resummarizeLoading[r.id] ? '...' : 'Resummarize'}</button>
                    <button class="btn-secondary btn !text-xs" disabled={retagLoading[r.id]} on:click={()=> triggerRetag(r.id)}>{retagLoading[r.id] ? '...' : 'Retag'}</button>
                  </div>
                </td>
              </tr>
            {/each}
          </tbody>
        </table>
      </div>
      {/if}
    </div>

    <!-- Add Document Panel -->
    <div class="panel">
      <h2 class="text-lg font-semibold tracking-tight">Add Document</h2>
      <textarea class="input w-full min-h-[170px] font-mono text-[12px] resize-y" rows="8" placeholder="Paste document text..." bind:value={newDocText}></textarea>
      <div class="flex flex-wrap gap-4 text-xs items-center">
        <label class="flex items-center gap-2"><input class="checkbox" type="checkbox" bind:checked={autoSummarize}/> <span>auto summarize</span></label>
        {#if autoSummarize}
          <label class="flex items-center gap-2">tokens <input class="input w-20" type="number" min="20" max="400" step="10" bind:value={summaryTokens} /></label>
        {/if}
        <label class="flex items-center gap-2"><input class="checkbox" type="checkbox" bind:checked={autoTag}/> <span>auto tag</span></label>
        {#if autoTag}
          <label class="flex items-center gap-2">max tags <input class="input w-20" type="number" min="1" max="20" step="1" bind:value={maxTags} /></label>
        {/if}
      </div>
      <div class="flex gap-2">
        <button class="btn" disabled={addLoading} on:click={addDoc}>{addLoading ? 'Embedding...' : 'Add'}</button>
        <button class="btn-secondary btn" type="button" on:click={()=> newDocText=''}>Clear</button>
      </div>
      {#if stats}
        <div class="grid grid-cols-3 gap-2 text-[11px] mt-2 text-slate-600">
          <div><span class="font-semibold text-slate-700">Docs</span> {stats.documents}</div>
          <div><span class="font-semibold text-slate-700">Backend</span> {stats.embedding_backend}</div>
          <div><span class="font-semibold text-slate-700">Dim</span> {stats.embedding_dimension}</div>
        </div>
      {/if}
    </div>
  </div>

  <!-- Database Explorer -->
  <div class="panel">
    <div class="flex items-center justify-between gap-4 flex-wrap">
      <h2 class="text-lg font-semibold tracking-tight">Database Explorer</h2>
      <div class="flex flex-wrap items-center gap-3 text-xs">
        <label class="flex items-center gap-1">Order
          <select class="input" bind:value={listOrder} on:change={() => { listOffset = 0; loadDocs(); }}>
            <option value="recent">recent</option>
            <option value="id">id</option>
          </select>
        </label>
        <label class="flex items-center gap-1">Limit <input class="input w-24" type="number" min="10" max="500" bind:value={listLimit} /></label>
        <button class="btn-secondary btn !text-xs" on:click={() => { listOffset = 0; loadDocs(); }} disabled={listLoading}>{listLoading ? 'Loading...' : 'Refresh'}</button>
        <div class="flex items-center gap-2">
          <button class="btn-secondary btn !text-xs" on:click={() => { listOffset = Math.max(listOffset - listLimit, 0); loadDocs(); }} disabled={listOffset===0 || listLoading}>Prev</button>
          <button class="btn-secondary btn !text-xs" on:click={() => { listOffset += listLimit; loadDocs(); }} disabled={listLoading}>Next</button>
        </div>
      </div>
    </div>
    {#if listLoading}
      <div class="text-slate-400 text-sm mt-3">Loading...</div>
    {:else if allDocs.length === 0}
      <div class="text-slate-400 text-sm mt-3">No documents.</div>
    {:else}
      <div class="overflow-auto max-h-[420px] mt-4 border border-slate-200 rounded-lg">
        <table class="data min-w-[1100px]">
          <thead>
            <tr>
              <th class="px-3 py-2">ID</th>
              <th class="px-3 py-2">Updated</th>
              <th class="px-3 py-2 w-[22%]">Text</th>
              <th class="px-3 py-2 w-[18%]">Summary</th>
              <th class="px-3 py-2 w-[14%]">Tags</th>
              <th class="px-3 py-2 w-[6%]">Len</th>
              <th class="px-3 py-2"></th>
            </tr>
          </thead>
          <tbody>
            {#each allDocs as d}
              <tr class="hover:bg-slate-50 transition">
                <td class="px-3 py-2"><span class="badge">{d.id}</span></td>
                <td class="px-3 py-2 text-[11px]">{formatTs(d.updated_at) || formatTs(d.created_at)}</td>
                <td class="px-3 py-2 text-[11px] whitespace-pre-wrap">{d.text.slice(0,240)}{d.text.length>240?'…':''}</td>
                <td class="px-3 py-2 text-[11px] whitespace-pre-wrap">{d.summary || '—'}</td>
                <td class="px-3 py-2">
                  {#if d.tags && d.tags.length}
                    <div class="flex flex-wrap">{#each d.tags as tg}<span class="tag-badge">{tg}</span>{/each}</div>
                  {:else}<span class="text-slate-400 text-[11px]">—</span>{/if}
                </td>
                <td class="px-3 py-2 text-[11px] tabular-nums">{d.text.length}</td>
                <td class="px-3 py-2">
                  <button class="btn-danger btn !text-xs" disabled={deleting[d.id]} on:click={() => deleteDoc(d.id)}>{deleting[d.id] ? '...' : 'Delete'}</button>
                </td>
              </tr>
            {/each}
          </tbody>
        </table>
      </div>
    {/if}
    <div class="mt-2 text-[11px] text-slate-500">Offset {listOffset}</div>
  </div>
</div>
