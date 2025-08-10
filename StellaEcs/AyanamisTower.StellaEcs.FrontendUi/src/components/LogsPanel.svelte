<script lang="ts">
  import { api, type LogEntry, type LogLevel } from '../lib/api';
  import { onMount } from 'svelte';

  let logs: LogEntry[] = [];
  let afterId = 0;
  let minLevel: LogLevel = 'Information';
  let category = '';
  let take = 200;
  let auto = true;
  let timer: any;

  async function loadOnce() {
    const page = await api.logs({ take, afterId, minLevel, category: category || undefined });
    if (page.length) {
      logs = [...logs, ...page];
      afterId = logs[logs.length - 1].id;
    }
  }

  function levelBg(l: LogLevel): string {
    switch (l) {
      case 'Information': return 'rgba(6,95,70,0.5)';
      case 'Warning': return 'rgba(113,63,18,0.5)';
      case 'Error':
      case 'Critical': return 'rgba(127,29,29,0.5)';
      default: return '';
    }
  }

  function toggleAuto() {
    auto = !auto;
    if (auto) start(); else stop();
  }
  function start() { stop(); timer = setInterval(loadOnce, 1000); }
  function stop() { if (timer) clearInterval(timer); }

  async function resetAndLoad() {
    logs = []; afterId = 0; await loadOnce();
  }

  async function clearRemote() {
    await api.clearLogs();
    await resetAndLoad();
  }

  onMount(() => { loadOnce(); start(); return stop; });
</script>

<div class="space-y-3">
  <div class="flex gap-2 items-end flex-wrap">
    <div>
      <label class="block text-xs text-zinc-400" for="minLevel">Min Level</label>
      <select id="minLevel" bind:value={minLevel} class="bg-zinc-900 border border-zinc-800 rounded px-2 py-1">
        <option>Trace</option>
        <option>Debug</option>
        <option selected>Information</option>
        <option>Warning</option>
        <option>Error</option>
        <option>Critical</option>
      </select>
    </div>
    <div>
      <label class="block text-xs text-zinc-400" for="category">Category contains</label>
      <input id="category" bind:value={category} class="bg-zinc-900 border border-zinc-800 rounded px-2 py-1" placeholder="e.g. AyanamisTower.StellaEcs" />
    </div>
    <div>
      <label class="block text-xs text-zinc-400" for="take">Take</label>
      <input id="take" type="number" min="1" max="2000" bind:value={take} class="bg-zinc-900 border border-zinc-800 rounded px-2 py-1 w-24" />
    </div>
    <button class="px-3 py-1 rounded bg-emerald-600 hover:bg-emerald-500" on:click={resetAndLoad}>Fetch</button>
    <button class="px-3 py-1 rounded bg-zinc-800 hover:bg-zinc-700" on:click={toggleAuto}>{auto ? 'Pause' : 'Auto'}</button>
    <button class="px-3 py-1 rounded bg-red-600 hover:bg-red-500" on:click={clearRemote}>Clear</button>
  </div>

  <div class="max-h-[400px] overflow-auto border border-zinc-800 rounded">
    <table class="w-full text-sm">
      <thead class="bg-zinc-900 sticky top-0">
        <tr>
          <th class="text-left p-2">Time</th>
          <th class="text-left p-2">Level</th>
          <th class="text-left p-2">Category</th>
          <th class="text-left p-2">Message</th>
        </tr>
      </thead>
      <tbody>
        {#each logs as l}
        <tr class="border-t border-zinc-800">
          <td class="p-2 text-zinc-400">{new Date(l.timestampUtc).toLocaleTimeString()}</td>
          <td class="p-2">
            <span class="px-2 py-0.5 rounded text-xs" style={`background-color:${levelBg(l.level)}`}>{l.level}</span>
          </td>
          <td class="p-2 text-zinc-400">{l.category}</td>
          <td class="p-2">
            <div class="whitespace-pre-wrap">{l.message}</div>
            {#if l.exception}
              <details class="mt-1 text-red-300">
                <summary>Exception</summary>
                <pre class="overflow-auto text-xs">{l.exception}</pre>
              </details>
            {/if}
          </td>
        </tr>
        {/each}
      </tbody>
    </table>
  </div>
</div>

<style>
  td, th { font-variant-numeric: tabular-nums; }
  pre { white-space: pre-wrap; }
  table { width: 100%; border-collapse: separate; border-spacing: 0; }
  thead th { position: sticky; top: 0; background: #0a0a0a; }
  tbody tr:nth-child(even) { background: rgba(255,255,255,0.01); }
</style>
