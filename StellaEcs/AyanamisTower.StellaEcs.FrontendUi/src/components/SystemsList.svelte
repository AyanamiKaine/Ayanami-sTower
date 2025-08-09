<script lang="ts">
    import { api, type SystemInfo } from "@lib/api";
    let systems: SystemInfo[] = [];
    let loading = false;
    let error: string | null = null;

    async function loadSystems() {
        loading = true;
        error = null;
        try {
            systems = await api.systems();
        } catch (e: any) {
            error = e.message;
        } finally {
            loading = false;
        }
    }

    loadSystems();
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
                <li class="flex gap-2 items-center">
                    <span>{s.name}</span>{#if s.pluginOwner}<span class="badge"
                            >{s.pluginOwner}</span
                        >{/if}{#if !s.enabled}<span class="text-red-400 text-xs"
                            >(disabled)</span
                        >{/if}
                </li>
            {/each}
        </ul>
    {/if}
</div>
