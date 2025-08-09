<script lang="ts">
    import type { EntitySummary } from "@lib/api";
    export let entities: EntitySummary[] = [];
    export let loading = false;
    export let onSelect: (idGen: string) => void;
</script>

<div class="card fade-in">
    <div class="flex items-center mb-3 gap-3">
        <h2 class="text-lg font-semibold">Entities</h2>
        <span class="badge">{entities.length}</span>
    </div>
    <div class="overflow-auto max-h-[60vh]">
        <table class="table text-sm">
            <thead>
                <tr><th class="pr-10">ID</th><th class="pr-10">Actions</th></tr>
            </thead>
            <tbody>
                {#if loading}
                    <tr
                        ><td colspan="2" class="py-6 text-center text-zinc-500"
                            >Loading...</td
                        ></tr
                    >
                {:else if entities.length === 0}
                    <tr
                        ><td colspan="2" class="py-6 text-center text-zinc-500"
                            >No entities</td
                        ></tr
                    >
                {:else}
                    {#each entities as e}
                        <tr
                            class="hover:bg-zinc-800/40 cursor-pointer"
                            on:click={() => onSelect(`${e.id}`)}
                        >
                            <td>{e.id}</td>
                            <td
                                ><button
                                    class="btn btn-primary"
                                    on:click|stopPropagation={() =>
                                        onSelect(`${e.id}`)}
                                    >Open</button
                                ></td
                            >
                        </tr>
                    {/each}
                {/if}
            </tbody>
        </table>
    </div>
</div>
