<script lang="ts">
    import type { WorldStatus } from "@lib/api";
    import { api } from "@lib/api";
    import { onMount } from "svelte";
    export let status: WorldStatus | null = null;
    let loading = false;
    let error: string | null = null;
    onMount(async () => {
        if (!status) {
            loading = true;
            try {
                status = await api.worldStatus();
            } catch (e: any) {
                error = e.message;
            } finally {
                loading = false;
            }
        }
    });
</script>

<div class="card grid gap-3 fade-in">
    <h2 class="text-lg font-semibold">World Status</h2>
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
        </div>
    {:else}
        <p class="text-sm text-zinc-400">No data.</p>
    {/if}
</div>
