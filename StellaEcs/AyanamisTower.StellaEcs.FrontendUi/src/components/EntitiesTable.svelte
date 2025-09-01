<script lang="ts">
    import type { EntitySummary } from "@lib/api";
    export let entities: EntitySummary[] = [];
    export let loading = false;
    export let onSelect: (idGen: string) => void;

    // Sorting state - only by ID for now
    let sortDir: 'asc' | 'desc' = 'asc';
    function toggleSort() {
        sortDir = sortDir === 'asc' ? 'desc' : 'asc';
    }

    $: sorted = [...entities].sort((a, b) =>
        sortDir === 'asc' ? a.id - b.id : b.id - a.id,
    );
    const sortIcon = () => (sortDir === 'asc' ? '▲' : '▼');
    
    import { beforeUpdate, afterUpdate } from 'svelte';
    let scrollEl: HTMLElement | null = null;
    let _savedScrollTop: number | null = null;

    beforeUpdate(() => {
        if (scrollEl) {
            // Save current scroll position before DOM updates so we can restore it
            _savedScrollTop = scrollEl.scrollTop;
        }
    });

    afterUpdate(() => {
        if (scrollEl && _savedScrollTop !== null) {
            // Restore saved scroll position. Use requestAnimationFrame to ensure layout done.
            requestAnimationFrame(() => {
                try {
                    scrollEl!.scrollTop = _savedScrollTop!;
                } catch (e) {
                    // ignore
                }
                _savedScrollTop = null;
            });
        }
    });
</script>

<div class="card fade-in">
    <div class="flex items-center mb-3 gap-3">
        <h2 class="text-lg font-semibold">Entities</h2>
        <span class="badge">{entities.length}</span>
    </div>
    <div class="overflow-auto max-h-[60vh]">
        <table class="table text-sm">
            <thead>
                <tr>
                    <th
                        class="pr-10 select-none cursor-pointer hover:text-emerald-400"
                        on:click={toggleSort}
                        aria-sort={sortDir === 'asc' ? 'ascending' : 'descending'}
                        title="Sort by ID"
                    >
                        <span class="inline-flex items-center gap-2">
                            <span>ID</span>
                            <span class="text-[10px] opacity-70">{sortIcon()}</span>
                        </span>
                    </th>
                </tr>
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
                    {#each sorted as e}
                        <tr
                            class="hover:bg-zinc-800/50 cursor-pointer focus:bg-zinc-800/70 outline-none"
                            tabindex="0"
                            role="button"
                            aria-label={`Open entity ${e.id}`}
                            on:click={() => onSelect(`${e.id}`)}
                            on:keydown={(ev) => { const k = ev.key; if (k === 'Enter' || k === ' ') { ev.preventDefault(); onSelect(`${e.id}`); } }}
                        >
                            <td class="py-3 px-3">
                                <div class="flex items-center">
                                    <span class="inline-block w-full">{e.id}</span>
                                </div>
                            </td>
                        </tr>
                    {/each}
                {/if}
            </tbody>
        </table>
    </div>
</div>
