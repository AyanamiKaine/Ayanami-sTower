<script lang="ts">
  import { onMount } from 'svelte';
  type Result = { id:number; text:string; summary?:string; tags?:string[]; embedding_task?: string | null; url?: string | null; token_count?: number | null; distance:number; created_at?:number; updated_at?:number };
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
  let maxDistance: number | null = null; // threshold filter (L2 distance)
  let newDocText = '';
  let addLoading = false;
  let autoSummarize = true;
  let summaryTokens = 80;
  let autoTag = true;
  let maxTags = 6;
  // Embedding task type selection (with descriptions & examples)
  const taskTypes = [
    'RETRIEVAL_DOCUMENT',
    'RETRIEVAL_QUERY',
    'SEMANTIC_SIMILARITY',
    'CLASSIFICATION',
    'CLUSTERING',
    'CODE_RETRIEVAL_QUERY',
    'QUESTION_ANSWERING',
    'FACT_VERIFICATION'
  ] as const;
  type TaskType = typeof taskTypes[number];
  interface TaskInfo { description: string; examples: string; guidance?: string }
  const taskTypeDetails: Record<TaskType, TaskInfo> = {
    RETRIEVAL_DOCUMENT: {
      description: 'Embeddings optimized for representing documents that will be retrieved.',
      examples: 'Indexing articles, books, web pages',
      guidance: 'Pair with RETRIEVAL_QUERY (or QUESTION_ANSWERING / CODE_RETRIEVAL_QUERY / FACT_VERIFICATION) for the query side.'
    },
    RETRIEVAL_QUERY: {
      description: 'Embeddings optimized for general natural language search queries.',
      examples: 'Custom semantic search boxes',
      guidance: 'Use this for user queries; ingest documents with RETRIEVAL_DOCUMENT.'
    },
    SEMANTIC_SIMILARITY: {
      description: 'Embeddings tuned to measure overall semantic similarity between texts.',
      examples: 'Recommendation systems, near-duplicate detection',
      guidance: 'Use when you mainly need to score similarity between arbitrary pairs of texts.'
    },
    CLASSIFICATION: {
      description: 'Embeddings shaped for downstream classification against labeled sets.',
      examples: 'Sentiment analysis, spam detection',
      guidance: 'Not ideal for retrieval ranking; better for feeding a classifier.'
    },
    CLUSTERING: {
      description: 'Embeddings optimized to group related texts via clustering algorithms.',
      examples: 'Document organization, market research, anomaly detection',
      guidance: 'Choose when you plan to cluster or visualize structure rather than direct search.'
    },
    CODE_RETRIEVAL_QUERY: {
      description: 'Embeddings for natural language queries over code/documentation corpora.',
      examples: 'Developer code search, code suggestion assistants',
      guidance: 'Use for user queries; still embed code blocks with RETRIEVAL_DOCUMENT.'
    },
    QUESTION_ANSWERING: {
      description: 'Embeddings for questions in a QA system optimized to retrieve answers.',
      examples: 'Chatbot questions, helpdesk queries',
      guidance: 'Use for the question side; embed answerable passages with RETRIEVAL_DOCUMENT.'
    },
    FACT_VERIFICATION: {
      description: 'Embeddings for claims/statements to retrieve supporting or refuting evidence.',
      examples: 'Automated fact checking pipelines',
      guidance: 'Embed claims with this; evidence passages with RETRIEVAL_DOCUMENT.'
    }
  };
  let taskType: TaskType = 'RETRIEVAL_DOCUMENT';
  $: currentTaskInfo = taskTypeDetails[taskType];
  // File upload state
  let uploadFile: File | null = null;
  let uploadLoading = false;
  let uploadResult: { id:number; summary?:string; tags?:string[]; embedding_task?: string | null } | null = null;
  // Separate task selection for file upload (can differ from text add)
  let uploadTaskType: TaskType = 'RETRIEVAL_DOCUMENT';
  $: if (!uploadTaskType) uploadTaskType = taskType; // safeguard
  let retagLoading: Record<number, boolean> = {};
  let resummarizeLoading: Record<number, boolean> = {};
  let refetchLoading: Record<number, boolean> = {};
  let stats: any = null;
  // Listing state
  interface ListedDoc { id:number; text:string; summary?:string|null; tags?:string[]|null; embedding_task?: string | null; url?: string | null; token_count?: number | null; created_at:number; updated_at:number }
  let allDocs: ListedDoc[] = [];
  let listLoading = false;
  let listOrder: 'id' | 'recent' = 'recent';
  let listLimit = 100;
  let listOffset = 0;
  let deleting: Record<number, boolean> = {};
  let error: string | null = null;
  // Web page ingestion state
  let webUrl = '';
  let webLoading = false;
  let webResult: { id:number; url:string; summary?:string; tags?:string[]; embedding_task?: string|null } | null = null;

  // Auth state
  interface AuthUser { id:number; email:string; isAdmin:boolean }
  let authUser: AuthUser | null = null;
  let authLoading = true;
  let loginEmail = '';
  let loginPassword = '';
  let registerEmail = '';
  let registerPassword = '';
  let registerPassword2 = '';
  let authError: string | null = null;
  let showRegister = false;
  let hasAdminFlag: boolean | null = null;
  let pwCurrent = '';
  let pwNew = '';
  let pwNew2 = '';
  let pwChanging = false;
  let pwMessage: string | null = null;
  // Admin user management state
  interface AdminUserRow { id:number; email:string; is_admin:boolean; is_approved:boolean; created_at:number; updated_at:number; last_login?:number|null }
  let adminUsers: AdminUserRow[] = [];
  let adminLoading = false;
  let adminError: string | null = null;
  let refreshingUsers = false;

  async function loadUsers() {
    if (!authUser?.isAdmin) return;
    adminLoading = true; adminError = null;
    try {
      const r = await fetch('/api/auth/users');
      const data = await r.json();
      if (!r.ok) throw new Error(data.error || 'failed to load users');
      adminUsers = data.users;
    } catch(e:any) { adminError = e.message; } finally { adminLoading = false; }
  }

  async function approveUser(id: number) {
    try {
      const r = await fetch('/api/auth/users', { method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify({ action:'approve', id }) });
      if (r.ok) {
        adminUsers = adminUsers.map(u => u.id===id ? { ...u, is_approved:true } : u);
      }
    } catch {}
  }

  async function disapproveUser(id: number) {
    try {
      const r = await fetch('/api/auth/users', { method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify({ action:'disapprove', id }) });
      if (r.ok) {
        adminUsers = adminUsers.map(u => u.id===id ? { ...u, is_approved:false } : u);
      }
    } catch {}
  }

  async function deleteUserAccount(id: number) {
    if (!confirm('Delete user '+id+'?')) return;
    try {
      const r = await fetch('/api/auth/users', { method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify({ action:'delete', id }) });
      if (r.ok) {
        adminUsers = adminUsers.filter(u => u.id !== id);
      }
    } catch {}
  }

  async function fetchMe() {
    authLoading = true;
    try {
      const r = await fetch('/api/auth/me');
      const data = await r.json();
      if (data.user) authUser = data.user; else authUser = null;
    } catch { authUser = null; }
    authLoading = false;
  }

  async function fetchStatus() {
    try { const r = await fetch('/api/auth/status'); const d = await r.json(); hasAdminFlag = !!d.has_admin; } catch { hasAdminFlag = null; }
  }

  async function login() {
    authError = null;
    try {
      const r = await fetch('/api/auth/login', { method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify({ email: loginEmail, password: loginPassword })});
      const data = await r.json();
      if (!r.ok) throw new Error(data.error || 'login failed');
      authUser = { id: data.user.id, email: data.user.email, isAdmin: data.user.is_admin };
      loginPassword='';
      await afterAuth();
    } catch(e:any) { authError = e.message; }
  }

  async function register() {
    authError = null;
    try {
      if (registerPassword !== registerPassword2) { authError = 'Passwords do not match'; return; }
      const r = await fetch('/api/auth/register', { method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify({ email: registerEmail, password: registerPassword })});
      const data = await r.json();
      if (!r.ok) throw new Error(data.error || 'register failed');
      if (data.is_admin && data.is_approved) {
        // First user scenario: auto-admin
        loginEmail = registerEmail;
        loginPassword = registerPassword;
        showRegister = false;
        await login();
        return;
      }
      showRegister = false; authError = 'Registered. Await admin approval.';
    } catch(e:any) { authError = e.message; }
  }

  async function logout() {
    await fetch('/api/auth/logout', { method:'POST' });
    authUser = null;
  }

  async function afterAuth() {
    fetchStats();
    loadDocs();
  }

  async function addWebPage() {
    if (!webUrl.trim()) return;
    webLoading = true; error = null; webResult = null;
    try {
      const body = { url: webUrl.trim(), task_type: taskType, auto_summarize: autoSummarize, summary_tokens: summaryTokens, auto_tag: autoTag, max_tags: maxTags };
      const r = await fetch('/api/add-web', { method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify(body)});
      const data = await r.json();
      if (!r.ok) throw new Error(data.error || 'add web failed');
      webResult = data; webUrl='';
      fetchStats(); loadDocs();
    } catch(e:any) { error = e.message; } finally { webLoading=false; }
  }

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
  const body: any = { query, top_k, rewrite_mode, use_recency, recency_half_life, recency_alpha, use_summary };
      if (maxDistance != null && maxDistance >= 0) body.max_distance = maxDistance;
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
      const r = await fetch('/api/documents', { method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify({ text: newDocText, task_type: taskType, auto_summarize: autoSummarize, summary_tokens: summaryTokens, auto_tag: autoTag, max_tags: maxTags })});
      const data = await r.json();
      if (!r.ok) throw new Error(data.error || 'add failed');
      newDocText='';
      fetchStats();
      // Refresh explorer list if visible
      loadDocs();
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

  async function triggerRefetch(id: number, hasUrl: boolean) {
    if (!hasUrl) return;
    refetchLoading[id] = true;
    try {
      const body: any = { id, resummarize: autoSummarize, retag: autoTag, summary_tokens: summaryTokens, max_tags: maxTags };
      const r = await fetch('/api/refetch', { method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify(body) });
      const data = await r.json();
      if (r.ok) {
        results = results.map(x => x.id === id ? { ...x, text: data.text ?? x.text, summary: data.summary ?? x.summary, tags: data.tags ?? x.tags } : x);
        allDocs = allDocs.map(x => x.id === id ? { ...x, text: data.text ?? x.text, summary: data.summary ?? x.summary, tags: data.tags ?? x.tags } : x);
      }
    } catch {} finally {
      refetchLoading[id] = false;
    }
  }

  onMount(async () => { await fetchMe(); if (authUser) { afterAuth(); if (authUser.isAdmin) loadUsers(); } });
  onMount(fetchStatus);

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

  async function uploadDocFile() {
    if (!uploadFile) return;
    uploadLoading = true; error = null; uploadResult = null;
    try {
      const form = new FormData();
      form.append('file', uploadFile);
  form.append('task_type', uploadTaskType || taskType);
      form.append('auto_summarize', String(autoSummarize));
      form.append('summary_tokens', String(summaryTokens));
      form.append('auto_tag', String(autoTag));
      form.append('max_tags', String(maxTags));
      const r = await fetch('/api/embed-file', { method:'POST', body: form });
      const data = await r.json().catch(()=>({}));
      if (!r.ok) throw new Error(data.error || 'upload failed');
      uploadResult = data;
      fetchStats();
      loadDocs();
      uploadFile = null;
    } catch(e:any) { error = e.message; } finally { uploadLoading = false; }
  }
</script>

<style>
  /* Legacy styles removed in favor of Tailwind utility classes */
</style>

<div class="max-w-[1500px] mx-auto px-6 py-6 flex flex-col gap-6">
  <header class="flex flex-col gap-2">
    <h1 class="text-2xl font-bold tracking-tight flex items-center gap-2">Vector Memory Console <span class="text-xs font-medium bg-brand-100 text-brand-700 px-2 py-0.5 rounded">Gemini</span></h1>
    <p class="text-sm text-slate-600">Search, summarize, tag and manage your local embedding store.</p>
    <div class="mt-2 flex items-center gap-3 text-xs">
      {#if authLoading}
        <span class="text-slate-500">Checking session...</span>
      {:else if authUser}
        <span class="text-slate-600">Signed in as <span class="font-semibold">{authUser.email}</span>{authUser.isAdmin ? ' (admin)':''}</span>
        <button class="btn-secondary btn !text-xs" on:click={logout}>Logout</button>
      {:else}
        <span class="text-slate-500">Not signed in</span>
      {/if}
    </div>
  </header>

  {#if !authUser}
    <div class="panel">
      <h2 class="text-lg font-semibold tracking-tight mb-2">{showRegister ? 'Register' : 'Login'}</h2>
      {#if hasAdminFlag === false}
        <div class="mb-3 text-[11px] rounded border border-amber-300 bg-amber-50 text-amber-800 p-2 leading-snug">
          No admin exists yet. The first account you register will automatically become the admin and be immediately active.
        </div>
      {/if}
      {#if authError}<div class="text-rose-600 text-sm mb-2">{authError}</div>{/if}
      {#if showRegister}
        <div class="flex flex-col gap-3 max-w-sm">
          <input class="input" placeholder="email" bind:value={registerEmail} />
          <input class="input" type="password" placeholder="password" bind:value={registerPassword} />
          <input class="input" type="password" placeholder="confirm password" bind:value={registerPassword2} />
          {#if registerPassword2 && registerPassword !== registerPassword2}
            <div class="text-[11px] text-rose-600">Passwords do not match</div>
          {/if}
          <div class="flex gap-2">
            <button class="btn" on:click={register} disabled={!registerEmail || !registerPassword || registerPassword !== registerPassword2}>Register</button>
            <button class="btn-secondary btn" on:click={() => { showRegister=false; authError=null; }}>Have account?</button>
          </div>
          <p class="text-[11px] text-slate-500">After registering an admin must approve your account before you can login.</p>
        </div>
      {:else}
        <div class="flex flex-col gap-3 max-w-sm">
          <input class="input" placeholder="email" bind:value={loginEmail} on:keydown={(e)=> e.key==='Enter' && login()} />
          <input class="input" type="password" placeholder="password" bind:value={loginPassword} on:keydown={(e)=> e.key==='Enter' && login()} />
          <div class="flex gap-2">
            <button class="btn" on:click={login} disabled={!loginEmail || !loginPassword}>Login</button>
            <button class="btn-secondary btn" on:click={() => { showRegister=true; authError=null; }}>Register</button>
          </div>
        </div>
      {/if}
    </div>
  {:else}

  <!-- Password Change Panel -->
  <div class="panel">
    <h2 class="text-lg font-semibold tracking-tight mb-3">Account Security</h2>
    <div class="flex flex-col gap-2 max-w-sm">
      {#if pwMessage}<div class="text-[11px] {pwMessage.startsWith('Error') ? 'text-rose-600':'text-emerald-600'}">{pwMessage}</div>{/if}
      <input class="input" type="password" placeholder="Current password" bind:value={pwCurrent} />
      <input class="input" type="password" placeholder="New password (min 6)" bind:value={pwNew} />
      <input class="input" type="password" placeholder="Confirm new password" bind:value={pwNew2} />
      {#if pwNew2 && pwNew !== pwNew2}
        <div class="text-[11px] text-rose-600">Passwords do not match</div>
      {/if}
      <div class="flex gap-2">
        <button class="btn" disabled={pwChanging || pwNew.length<6 || !pwCurrent || pwNew !== pwNew2} on:click={async ()=>{ pwMessage=null; if (pwNew !== pwNew2) { pwMessage='Error: passwords do not match'; return;} pwChanging=true; try { const r= await fetch('/api/auth/change-password',{method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify({ current_password: pwCurrent, new_password: pwNew })}); const d= await r.json(); if(!r.ok) throw new Error(d.error||'failed'); pwMessage='Password updated'; pwCurrent=''; pwNew=''; pwNew2=''; } catch(e:any){ pwMessage='Error: '+e.message;} finally { pwChanging=false;} }}>
          {pwChanging ? 'Updating...' : 'Change Password'}
        </button>
      </div>
      <p class="text-[10px] text-slate-500 leading-snug">Change your password regularly. Sessions remain valid until expiry.</p>
    </div>
  </div>

  {#if authUser.isAdmin}
    <div class="panel">
      <div class="flex items-center justify-between mb-3 flex-wrap gap-2">
        <h2 class="text-lg font-semibold tracking-tight">Admin – Users</h2>
        <div class="flex gap-2 items-center text-xs">
          <button class="btn-secondary btn !text-xs" on:click={loadUsers} disabled={adminLoading}>{adminLoading ? 'Refreshing...' : 'Refresh'}</button>
          <span class="text-slate-500">{adminUsers.length} users</span>
        </div>
      </div>
      {#if adminError}<div class="text-rose-600 text-xs mb-2">{adminError}</div>{/if}
      {#if adminLoading && adminUsers.length===0}
        <div class="text-slate-400 text-sm">Loading users...</div>
      {:else if adminUsers.length===0}
        <div class="text-slate-400 text-sm">No users found.</div>
      {:else}
        <div class="overflow-auto max-h-[260px] border border-slate-200 rounded-lg">
          <table class="data min-w-[900px]">
            <thead>
              <tr>
                <th class="px-3 py-2">ID</th>
                <th class="px-3 py-2">Email</th>
                <th class="px-3 py-2">Approved</th>
                <th class="px-3 py-2">Admin</th>
                <th class="px-3 py-2">Created</th>
                <th class="px-3 py-2">Updated</th>
                <th class="px-3 py-2">Last Login</th>
                <th class="px-3 py-2"></th>
              </tr>
            </thead>
            <tbody>
              {#each adminUsers as u}
                <tr class="hover:bg-slate-50 transition">
                  <td class="px-3 py-2 text-[11px] tabular-nums">{u.id}</td>
                  <td class="px-3 py-2 text-[11px] font-mono max-w-[220px] truncate" title={u.email}>{u.email}</td>
                  <td class="px-3 py-2 text-[11px]">{u.is_approved ? 'yes' : 'no'}</td>
                  <td class="px-3 py-2 text-[11px]">{u.is_admin ? 'yes' : 'no'}</td>
                  <td class="px-3 py-2 text-[10px]">{formatTs(u.created_at)}</td>
                  <td class="px-3 py-2 text-[10px]">{formatTs(u.updated_at)}</td>
                  <td class="px-3 py-2 text-[10px]">{u.last_login ? formatTs(u.last_login) : '—'}</td>
                  <td class="px-3 py-2">
                    <div class="flex flex-wrap gap-1">
                      {#if !u.is_approved}
                        <button class="btn-secondary btn !text-[10px]" on:click={()=> approveUser(u.id)}>Approve</button>
                      {:else}
                        {#if !u.is_admin}
                          <button class="btn-secondary btn !text-[10px]" on:click={()=> disapproveUser(u.id)}>Disapprove</button>
                        {/if}
                      {/if}
                      {#if !u.is_admin}
                        <button class="btn-danger btn !text-[10px]" on:click={()=> deleteUserAccount(u.id)}>Delete</button>
                      {/if}
                    </div>
                  </td>
                </tr>
              {/each}
            </tbody>
          </table>
        </div>
        <div class="mt-2 text-[10px] text-slate-500">Approve users to grant access. Deleting removes their sessions implicitly.</div>
      {/if}
    </div>
  {/if}

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
        <label class="flex items-center gap-2" title="Filter out results with distance greater than this (L2)">Max Dist <input class="input w-24" type="number" min="0" step="0.01" bind:value={maxDistance} placeholder="none" /></label>
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
              <th class="px-3 py-2">Task</th>
              <th class="px-3 py-2">Dist</th>
              <th class="px-3 py-2">Updated</th>
              <th class="px-3 py-2">URL</th>
              <th class="px-3 py-2 w-[24%]">Text</th>
              <th class="px-3 py-2 w-[22%]">Summary</th>
              <th class="px-3 py-2 w-[12%]">Tags</th>
              <th class="px-3 py-2">Len / Tok</th>
              <th class="px-3 py-2"></th>
            </tr>
          </thead>
          <tbody>
            {#each results as r}
              <tr class="hover:bg-slate-50 transition">
                <td class="px-3 py-2"><span class="badge">{r.id}</span></td>
                <td class="px-3 py-2 text-[10px] font-mono max-w-[90px] truncate" title={r.embedding_task || ''}>{r.embedding_task || '—'}</td>
                <td class="px-3 py-2 text-[11px] tabular-nums">{r.distance.toFixed(4)}</td>
                <td class="px-3 py-2 text-[11px]">{formatTs(r.updated_at) || formatTs(r.created_at)}</td>
                <td class="px-3 py-2 text-[10px] max-w-[120px] truncate" title={r.url || ''}>{r.url ? r.url.replace(/^https?:\/\//,'').slice(0,60) : '—'}</td>
                <td class="px-3 py-2 text-xs whitespace-pre-wrap">{r.text}</td>
                <td class="px-3 py-2 text-[11px] whitespace-pre-wrap">{r.summary || '—'}</td>
                <td class="px-3 py-2">
                  {#if r.tags}
                    <div class="flex flex-wrap">{#each r.tags as tg}<span class="tag-badge">{tg}</span>{/each}</div>
                  {:else} <span class="text-slate-400 text-[11px]">—</span>{/if}
                </td>
                <td class="px-3 py-2 text-[10px] tabular-nums">
                  {r.text.length}{#if r.token_count} / {r.token_count}{/if}
                </td>
                <td class="px-3 py-2">
                  <div class="flex flex-col gap-1">
                    <button class="btn-secondary btn !text-xs" disabled={resummarizeLoading[r.id]} on:click={()=> triggerResummarize(r.id)}>{resummarizeLoading[r.id] ? '...' : 'Resummarize'}</button>
                    <button class="btn-secondary btn !text-xs" disabled={retagLoading[r.id]} on:click={()=> triggerRetag(r.id)}>{retagLoading[r.id] ? '...' : 'Retag'}</button>
                    {#if r.url}
                      <button class="btn-secondary btn !text-xs" disabled={refetchLoading[r.id]} on:click={()=> triggerRefetch(r.id, !!r.url)}>{refetchLoading[r.id] ? '...' : 'Re-fetch'}</button>
                    {/if}
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
      <div class="flex flex-col gap-3">
        <div class="flex flex-wrap gap-4 items-end">
          <div class="flex-1 min-w-[180px]">
            <label for="taskType" class="text-[11px] font-medium text-slate-600 uppercase tracking-wide flex items-center gap-1">Embedding Task
              <span class="inline-block text-[10px] font-normal text-slate-400 normal-case">(choose intent)</span>
            </label>
            <select id="taskType" class="input w-full mt-2" bind:value={taskType}>
              {#each taskTypes as t}
                <option value={t} title={`${taskTypeDetails[t].description} Examples: ${taskTypeDetails[t].examples}`}>{t}</option>
              {/each}
            </select>
            <div class="mt-2 mb-2 rounded border border-slate-200 bg-slate-50 p-2 flex flex-col gap-1">
              <div class="text-[11px] font-semibold text-slate-700 flex items-center gap-2">
                {taskType}
                <span class="text-[10px] font-normal px-1 py-0.5 rounded bg-slate-200 text-slate-600">info</span>
              </div>
              <div class="text-[11px] text-slate-600 leading-snug">{currentTaskInfo.description}</div>
              <div class="text-[10px] text-slate-500"><span class="font-medium">Examples:</span> {currentTaskInfo.examples}</div>
              {#if currentTaskInfo.guidance}
                <div class="text-[10px] text-slate-500 italic">{currentTaskInfo.guidance}</div>
              {/if}
            </div>
          </div>
        </div>
      </div>
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
      <div class="mt-6 border-t border-slate-200 pt-4 flex flex-col gap-3">
        <h3 class="text-sm font-semibold tracking-wide text-slate-700 uppercase">Upload File</h3>
        <div class="flex flex-wrap gap-3 items-end">
          <div class="min-w-[160px]">
            <label for="uploadTask" class="text-[10px] font-medium text-slate-600 uppercase tracking-wide">Task (file)</label>
            <select id="uploadTask" class="input w-full mt-1" bind:value={uploadTaskType}>
              {#each taskTypes as t}
                <option value={t}>{t}</option>
              {/each}
            </select>
          </div>
          <div class="flex-1 text-[10px] text-slate-500 leading-snug max-w-[420px]">
            Ingest file with its own embedding intent. This does not change the Add Document selector above.
          </div>
        </div>
        <input class="input w-full" type="file" on:change={(e:any)=> { const f=e.target.files?.[0]; uploadFile = f || null; uploadResult=null; }} />
        <div class="flex gap-2">
          <button class="btn" disabled={uploadLoading || !uploadFile} on:click={uploadDocFile}>{uploadLoading ? 'Uploading...' : 'Upload & Embed'}</button>
          {#if uploadFile}<span class="text-[11px] text-slate-500">{uploadFile.name} ({Math.round(uploadFile.size/1024)} KB)</span>{/if}
        </div>
        {#if uploadResult}
          <div class="text-[11px] text-green-600 flex flex-wrap items-center gap-1">
            <span>Stored id {uploadResult.id}</span>
            {#if uploadResult.embedding_task || uploadTaskType}
              <span class="text-slate-400">•</span>
              <span class="font-mono text-[10px] px-1 py-0.5 rounded bg-emerald-50 border border-emerald-200 text-emerald-700">{uploadResult.embedding_task || uploadTaskType}</span>
            {/if}
            {#if uploadResult.summary}<span class="text-slate-400">•</span><span>summary</span>{/if}
            {#if uploadResult.tags && uploadResult.tags.length}<span class="text-slate-400">•</span><span>{uploadResult.tags.length} tags</span>{/if}
          </div>
        {/if}
      </div>
      <div class="mt-6 border-t border-slate-200 pt-4 flex flex-col gap-3">
        <h3 class="text-sm font-semibold tracking-wide text-slate-700 uppercase">Add Web Page</h3>
        <div class="flex flex-col gap-2">
          <input class="input w-full" placeholder="https://example.com/article" bind:value={webUrl} on:keydown={(e)=> e.key==='Enter' && addWebPage()} />
          <div class="flex gap-2 items-center">
            <button class="btn" disabled={webLoading || !webUrl.trim()} on:click={addWebPage}>{webLoading ? 'Fetching...' : 'Fetch & Embed'}</button>
            {#if webResult}
              <div class="text-[11px] text-green-600 flex flex-wrap items-center gap-1">
                <span>ID {webResult.id}</span>
                {#if webResult.embedding_task}<span class="text-slate-400">•</span><span class="font-mono text-[10px] px-1 py-0.5 rounded bg-emerald-50 border border-emerald-200 text-emerald-700">{webResult.embedding_task}</span>{/if}
                <span class="text-slate-400">•</span><span class="truncate max-w-[220px]" title={webResult.url}>{webResult.url}</span>
              </div>
            {/if}
          </div>
          <div class="text-[10px] text-slate-500 leading-snug">Page HTML is cleaned (scripts/styles removed) and truncated if very large. Auto summarize/tag options above apply.</div>
        </div>
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
  <table class="data min-w-[1280px]">
          <thead>
            <tr>
              <th class="px-3 py-2">ID</th>
              <th class="px-3 py-2">Task</th>
              <th class="px-3 py-2">Updated</th>
              <th class="px-3 py-2 w-[16%]">URL</th>
              <th class="px-3 py-2 w-[20%]">Text</th>
              <th class="px-3 py-2 w-[16%]">Summary</th>
              <th class="px-3 py-2 w-[12%]">Tags</th>
              <th class="px-3 py-2 w-[6%]">Len</th>
              <th class="px-3 py-2 w-[6%]">Tokens</th>
              <th class="px-3 py-2"></th>
            </tr>
          </thead>
          <tbody>
            {#each allDocs as d}
              <tr class="hover:bg-slate-50 transition">
                <td class="px-3 py-2"><span class="badge">{d.id}</span></td>
                <td class="px-3 py-2 text-[10px] font-mono max-w-[90px] truncate" title={d.embedding_task || ''}>{d.embedding_task || '—'}</td>
                <td class="px-3 py-2 text-[11px]">{formatTs(d.updated_at) || formatTs(d.created_at)}</td>
                <td class="px-3 py-2 text-[10px] whitespace-pre-wrap max-w-[200px]">{d.url ? d.url : '—'}</td>
                <td class="px-3 py-2 text-[11px] whitespace-pre-wrap">{d.text.slice(0,200)}{d.text.length>200?'…':''}</td>
                <td class="px-3 py-2 text-[11px] whitespace-pre-wrap">{d.summary || '—'}</td>
                <td class="px-3 py-2">
                  {#if d.tags && d.tags.length}
                    <div class="flex flex-wrap">{#each d.tags as tg}<span class="tag-badge">{tg}</span>{/each}</div>
                  {:else}<span class="text-slate-400 text-[11px]">—</span>{/if}
                </td>
                <td class="px-3 py-2 text-[11px] tabular-nums">{d.text.length}</td>
                <td class="px-3 py-2 text-[11px] tabular-nums">{d.token_count ?? '—'}</td>
                <td class="px-3 py-2">
                  <div class="flex flex-col gap-1">
                    {#if d.url}
                      <button class="btn-secondary btn !text-xs" disabled={refetchLoading[d.id]} on:click={()=> triggerRefetch(d.id, !!d.url)}>{refetchLoading[d.id] ? '...' : 'Re-fetch'}</button>
                    {/if}
                    <button class="btn-danger btn !text-xs" disabled={deleting[d.id]} on:click={() => deleteDoc(d.id)}>{deleting[d.id] ? '...' : 'Delete'}</button>
                  </div>
                </td>
              </tr>
            {/each}
          </tbody>
        </table>
      </div>
    {/if}
    <div class="mt-2 text-[11px] text-slate-500">Offset {listOffset}</div>
  </div>
  {/if}
</div>
