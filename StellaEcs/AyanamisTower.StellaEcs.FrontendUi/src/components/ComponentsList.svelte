<script lang="ts">
    import { api, type ComponentInfo } from "@lib/api";
    let components: ComponentInfo[] = [];
    let loading = false;
    let error: string | null = null;

    async function loadComponents() {
        loading = true;
        error = null;
        try {
            components = await api.components();
        } catch (e: any) {
            error = e.message;
        } finally {
            loading = false;
        }
    }

    loadComponents();
</script>

<div class="card fade-in">
    <div class="flex items-center mb-3 gap-3">
        <h2 class="text-lg font-semibold">Component Types</h2>
        <button class="btn" on:click={loadComponents} disabled={loading}>
            Refresh
        </button>
    </div>
    {#if error}
        <p class="text-red-400 text-sm mb-3">{error}</p>
    {/if}
    {#if loading}
        <p class="text-zinc-500 text-sm">Loading...</p>
    {:else if components.length === 0}
        <p class="text-zinc-500 text-sm">No component types</p>
    {:else}
        <ul class="grid gap-2 text-sm">
            {#each components as c}
                <li class="flex gap-2 items-center">
                    <span>{c.typeName}</span>{#if c.pluginOwner}<span
                            class="badge">{c.pluginOwner}</span
                        >{/if}
                </li>
            {/each}
        </ul>
    {/if}
</div>
