<script lang="ts">
    import { api, type SystemInfo } from "@lib/api";
    let systems: SystemInfo[] = [];
    let loading = false;
    let error: string | null = null;
    let working: Record<string, boolean> = {};
    let timings: Record<string, { lastMs: number; avgMs: number; calls: number }> = {};
    let loadingTimings = false;

    async function loadSystems() {
        loading = true;
        error = null;
        try {
            systems = await api.systems();
            // ensure sorted by Group then Order for consistent display
            systems.sort((a: any, b: any) => (a.group ?? '').localeCompare(b.group ?? '') || (a.order ?? 0) - (b.order ?? 0));
        } catch (e: any) {
            error = e.message;
        } finally {
            loading = false;
        }
    }

    async function loadTimings() {
        loadingTimings = true;
        try {
            const list = await api.systemTimings();
            const map: Record<string, { lastMs: number; avgMs: number; calls: number }> = {};
            for (const s of list) map[s.name] = { lastMs: s.lastMs, avgMs: s.avgMs, calls: s.calls };
            timings = map;
        } catch {
            // ignore
        } finally {
            loadingTimings = false;
        }
    }

    // Refresh timings periodically while the view is open
    let timingsTimer: any = null;
    import { onDestroy } from 'svelte';
    // initial load
    loadSystems();
    loadTimings();
    timingsTimer = setInterval(() => loadTimings(), 2000);
    onDestroy(() => { if (timingsTimer) clearInterval(timingsTimer); });

    async function disableSystem(name: string) {
        if (working[name]) return;
        working[name] = true; working = { ...working };
        try {
            await api.disableSystem(name);
            // Optimistically mark as disabled in UI
            systems = systems.map(s => s.name === name ? { ...s, enabled: false } : s);
        } catch (e: any) {
            error = e.message;
        } finally {
            working[name] = false; working = { ...working };
        }
    }

    async function enableSystem(name: string) {
        if (working[name]) return;
        working[name] = true; working = { ...working };
        try {
            await api.enableSystem(name);
            systems = systems.map(s => s.name === name ? { ...s, enabled: true } : s);
        } catch (e: any) {
            error = e.message;
        } finally {
            working[name] = false; working = { ...working };
        }
    }
</script>

<div class="card fade-in">
    <div class="flex items-center mb-3 gap-3">
        <h2 class="text-lg font-semibold">Systems</h2>
        <button class="btn" on:click={loadSystems} disabled={loading}>
            Refresh
        </button>
    </div>
    {#if error}
        <p class="text-red-400 text-sm mb-3">{error}</p>
    {/if}
    {#if loading}
        <p class="text-zinc-500 text-sm">Loading...</p>
    {:else if systems.length === 0}
        <p class="text-zinc-500 text-sm">No systems</p>
    {:else}
        <ul class="grid gap-2 text-sm">
            {#each systems as s}
                <li class="flex gap-3 items-center">
                    <span class="font-medium">{s.name}</span>
                    {#if s.group}
                        <span class="badge" title="Group">{s.group.replace('SystemGroup','')}</span>
                        <span class="text-xs text-zinc-400" title="Order">
                            #{s.order}
                        </span>
                    {/if}
                    {#if s.pluginOwner}<span class="badge">{s.pluginOwner}</span>{/if}
                    {#if !s.enabled}<span class="text-red-400 text-xs">(disabled)</span>{/if}
                    {#if timings[s.name]}
                        <div class="text-xs text-zinc-400 ml-3">
                            Last: {timings[s.name].lastMs.toFixed(2)} ms · Avg: {timings[s.name].avgMs.toFixed(2)} ms · Calls: {timings[s.name].calls}
                        </div>
                    {/if}
                    <span class="ml-auto" />
                    <div class="flex gap-2">
                        {#if s.enabled}
                            <button class="btn text-xs" on:click={() => disableSystem(s.name)} disabled={!!working[s.name]} title="Disable system">
                                {working[s.name] ? 'Disabling…' : 'Disable'}
                            </button>
                        {:else}
                            <button class="btn text-xs" on:click={() => enableSystem(s.name)} disabled={!!working[s.name]} title="Enable system">
                                {working[s.name] ? 'Enabling…' : 'Enable'}
                            </button>
                        {/if}
                    </div>
                </li>
            {/each}
        </ul>
    {/if}
</div>
