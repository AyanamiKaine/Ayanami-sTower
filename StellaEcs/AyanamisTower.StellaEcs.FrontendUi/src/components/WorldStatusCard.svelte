<script lang="ts">
    import type { WorldStatus } from "@lib/api";
    import { api } from "@lib/api";
    import { onMount } from "svelte";
    export let status: WorldStatus | null = null;
    let loading = false;
    let error: string | null = null;
    let stepping = false;
    let stepFrames = 1;
    let stepDt: number | null = null;

    async function refresh() {
        loading = true;
        try {
            status = await api.worldStatus();
        } catch (e: any) {
            error = e.message;
        } finally {
            loading = false;
        }
    }

    async function pause() {
        try {
            await api.pauseWorld();
            await refresh();
        } catch (e: any) { error = e.message; }
    }
    async function resume() {
        try {
            await api.resumeWorld();
            await refresh();
        } catch (e: any) { error = e.message; }
    }
    async function step() {
        if (!status) return;
        stepping = true;
        try {
            await api.stepWorld(stepFrames || 1, stepDt ?? undefined);
            await refresh();
        } catch (e: any) { error = e.message; }
        finally { stepping = false; }
    }
    onMount(async () => {
        if (!status) await refresh();
    });
</script>

<div class="card grid gap-3 fade-in">
    <div class="flex items-center gap-2">
        <h2 class="text-lg font-semibold">World Status</h2>
        {#if status?.isPaused}
            <span class="px-2 py-0.5 rounded bg-yellow-900/40 text-yellow-300 text-xs">Paused</span>
        {/if}
    </div>
    {#if error}
        <p class="text-sm text-red-400">{error}</p>
    {:else if loading}
        <p class="text-sm text-zinc-400">Loading...</p>
    {:else if status}
        <div class="grid gap-2 text-sm">
            <div class="flex gap-2">
                <span class="text-zinc-400 w-40">Max Entities</span><span
                    >{status.maxEntities}</span
                >
            </div>
            <div class="flex gap-2">
                <span class="text-zinc-400 w-40">Tick</span><span
                    >{status.tick}</span
                >
            </div>
            <div class="flex gap-2">
                <span class="text-zinc-400 w-40">Delta Time</span><span
                    >{status.deltaTime.toFixed(4)} s</span
                >
            </div>
            <div class="flex gap-2">
                <span class="text-zinc-400 w-40">Recycled IDs</span><span
                    >{status.recycledEntityIds}</span
                >
            </div>
            <div class="flex gap-2">
                <span class="text-zinc-400 w-40">Systems</span><span
                    >{status.registeredSystems}</span
                >
            </div>
            <div class="flex gap-2">
                <span class="text-zinc-400 w-40">Component Types</span><span
                    >{status.componentTypes}</span
                >
            </div>
            <div class="mt-3 flex flex-wrap gap-2">
                {#if !status.isPaused}
                    <button class="btn btn-xs" on:click={pause} aria-label="Pause">Pause</button>
                {:else}
                    <button class="btn btn-xs" on:click={resume} aria-label="Resume">Resume</button>
                    <div class="flex items-center gap-2">
                        <label class="text-zinc-400 text-xs" for="frames">Frames</label>
                        <input id="frames" type="number" min="1" class="input input-xs w-20" bind:value={stepFrames} />
                        <label class="text-zinc-400 text-xs" for="dt">dt (s)</label>
                        <input id="dt" type="number" step="0.0001" class="input input-xs w-28" bind:value={stepDt} placeholder={status.deltaTime.toFixed(4)} />
                        <button class="btn btn-xs" disabled={stepping} on:click={step}>{stepping ? 'Stepping...' : 'Step'}</button>
                    </div>
                {/if}
                <button class="btn btn-xs" on:click={refresh} aria-label="Refresh">Refresh</button>
            </div>
        </div>
    {:else}
        <p class="text-sm text-zinc-400">No data.</p>
    {/if}
</div>
