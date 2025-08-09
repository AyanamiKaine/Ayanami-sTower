<script lang="ts">
    import { api, type ServiceInfo } from "@lib/api";
    let services: ServiceInfo[] = [];
    let loading = false;
    let error: string | null = null;
    export let onInvoke: (service: string, method: string) => void = () => {};

    async function loadServices() {
        loading = true;
        error = null;
        try {
            services = await api.services();
        } catch (e: any) {
            error = e.message;
        } finally {
            loading = false;
        }
    }

    loadServices();
</script>

<div class="card fade-in">
    <div class="flex items-center mb-3 gap-3">
        <h2 class="text-lg font-semibold">Services</h2>
        <button class="btn" on:click={loadServices} disabled={loading}>
            Refresh
        </button>
    </div>
    {#if error}
        <p class="text-red-400 text-sm mb-3">{error}</p>
    {/if}
    {#if loading}
        <p class="text-zinc-500 text-sm">Loading...</p>
    {:else if services.length === 0}
        <p class="text-zinc-500 text-sm">No services</p>
    {:else}
        <ul class="grid gap-3 text-sm">
            {#each services as s}
                <li class="p-2 border border-zinc-700 rounded">
                    <div class="flex gap-2 items-center mb-1">
                        <span class="font-medium">{s.typeName}</span
                        >{#if s.pluginOwner}<span class="badge"
                                >{s.pluginOwner}</span
                            >{/if}
                    </div>
                    <div class="flex flex-wrap gap-2">
                        {#each s.methods as m}
                            <button
                                class="btn"
                                on:click={() => onInvoke(s.typeName, m)}
                                >{m}</button
                            >
                        {/each}
                    </div>
                </li>
            {/each}
        </ul>
    {/if}
</div>
