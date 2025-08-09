<script lang="ts">
    import { api, type PluginInfo } from "@lib/api";
    let plugins: PluginInfo[] = [];
    let loading = false;
    let error: string | null = null;
    export let onOpen: ((prefix: string) => void) | undefined = undefined;

    function open(prefix: string) {
        if (onOpen) onOpen(prefix);
    }

    async function loadPlugins() {
        loading = true;
        error = null;
        try {
            plugins = await api.plugins();
        } catch (e: any) {
            error = e.message;
        } finally {
            loading = false;
        }
    }

    loadPlugins();
</script>

<div class="card fade-in">
    <div class="flex items-center mb-3 gap-3">
        <h2 class="text-lg font-semibold">Plugins</h2>
        <button class="btn" on:click={loadPlugins} disabled={loading}>
            Refresh
        </button>
    </div>
    {#if error}
        <p class="text-red-400 text-sm mb-3">{error}</p>
    {/if}
    {#if loading}
        <p class="text-zinc-500 text-sm">Loading...</p>
    {:else if plugins.length === 0}
        <p class="text-zinc-500 text-sm">No plugins</p>
    {:else}
        <div class="grid gap-3">
            {#each plugins as p}
                <a
                    class="block p-3 rounded border border-zinc-700 hover:border-emerald-500/40 cursor-pointer focus:outline-none focus:ring-2 focus:ring-emerald-500/50"
                    href={`/plugins/${p.prefix}`}
                    on:click={() => open(p.prefix)}
                >
                    <div class="flex gap-2 items-center mb-1">
                        <span class="font-medium">{p.name}</span>
                        <span class="badge">v{p.version}</span>
                        <span class="text-xs text-zinc-500">{p.author}</span>
                    </div>
                    <p class="text-xs text-zinc-400 line-clamp-2">
                        {p.description}
                    </p>
                </a>
            {/each}
        </div>
    {/if}
</div>
