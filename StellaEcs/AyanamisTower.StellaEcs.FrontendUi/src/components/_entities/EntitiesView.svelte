<script lang="ts">
    import { api, type EntitySummary, type EntityDetail } from "@lib/api";
    import EntitiesTable from "../EntitiesTable.svelte";
    import EntityDetailPanel from "../EntityDetailPanel.svelte";
    import AddComponentForm from "../AddComponentForm.svelte";
    let entities: EntitySummary[] = [];
    let selectedIdGen: string | null = null;
    let detail: EntityDetail | null = null;
    let loading = false;
    let creating = false;
    let error: string | null = null;

    // Edit mode state
    let editMode = false;
    let editComponentType = "";
    let editComponentData: any = null;

    // Auto-reload state
    let autoReloadEnabled = false;
    let autoReloadInterval = 5; // seconds
    let autoReloadTimer: any = null;
    let isRefreshing = false; // prevent overlapping refreshes

    // Auto-reload interval options
    const intervalOptions = [
        { label: "1 second", value: 1 },
        { label: "5 seconds", value: 5 },
        { label: "10 seconds", value: 10 },
        { label: "30 seconds", value: 30 },
        { label: "1 minute", value: 60 },
        { label: "5 minutes", value: 300 },
    ];

    async function loadEntities() {
        loading = true;
        error = null;
        try {
            entities = await api.entities();
        } catch (e: any) {
            error = e.message;
        } finally {
            loading = false;
        }
    }

    // Auto-reload management
    function startAutoReload() {
        stopAutoReload();
        if (autoReloadEnabled && autoReloadInterval > 0) {
            autoReloadTimer = setInterval(() => {
                if (selectedIdGen) {
                    // Soft-refresh the selected entity without resetting UI state
                    refreshSelectedEntity();
                } else {
                    // Reload the entities list
                    loadEntities();
                }
            }, autoReloadInterval * 1000);
        }
    }

    function stopAutoReload() {
        if (autoReloadTimer) {
            clearInterval(autoReloadTimer);
            autoReloadTimer = null;
        }
    }

    // Reactive statements for auto-reload
    $: if (autoReloadEnabled) {
        startAutoReload();
    } else {
        stopAutoReload();
    }

    // Restart timer when interval changes
    $: if (autoReloadEnabled && autoReloadInterval) {
        startAutoReload();
    }

    // Cleanup on component destroy
    import { onDestroy } from "svelte";
    onDestroy(() => {
        stopAutoReload();
    });

    // Soft refresh for current selection to avoid flicker/state resets
    async function refreshSelectedEntity() {
        if (!selectedIdGen || isRefreshing) return;
        isRefreshing = true;
        try {
            const latest = await api.entity(selectedIdGen);
            // Update detail in place without touching edit UI state
            detail = latest;
        } catch (e: any) {
            // Keep auto-reload quiet on transient errors; surface only if user triggered
            // Optionally: if 404, clear selection
            // error = e.message;
        } finally {
            isRefreshing = false;
        }
    }

    async function select(idGen: string) {
        selectedIdGen = idGen;
        error = null;

        // Reset edit mode when selecting a different entity
        editMode = false;
        editComponentType = "";
        editComponentData = null;

        try {
            detail = await api.entity(idGen);
        } catch (e: any) {
            error = e.message;
        }
    }
    async function createEntity() {
        creating = true;
        error = null;
        try {
            const e = await api.createEntity();
            entities = [e, ...entities];
            await select(`${e.id}-${e.generation}`);
        } catch (e: any) {
            error = e.message;
        } finally {
            creating = false;
        }
    }
    async function removeComponent(type: string) {
        if (!selectedIdGen) return;
        try {
            await api.removeComponent(selectedIdGen, type);
            if (detail)
                detail.components = detail.components.filter(
                    (c) => c.typeName !== type,
                );
        } catch (e: any) {
            error = e.message;
        }
    }
    async function applyComponent(type: string, data: any) {
        if (!selectedIdGen) return;
        try {
            await api.addComponent(selectedIdGen, type, data);
            await select(selectedIdGen);

            // Reset edit mode after successful update
            if (editMode) {
                editMode = false;
                editComponentType = "";
                editComponentData = null;
            }
        } catch (e: any) {
            error = e.message;
        }
    }
    async function updateComponent(type: string, newData: any) {
        if (!selectedIdGen) return;
        try {
            await api.addComponent(selectedIdGen, type, newData);
            await select(selectedIdGen);
        } catch (e: any) {
            error = e.message;
        }
    }
    async function delEntity() {
        if (!selectedIdGen) return;
        try {
            await api.deleteEntity(selectedIdGen);
            entities = entities.filter(
                (e) => `${e.id}-${e.generation}` !== selectedIdGen,
            );
            selectedIdGen = null;
            detail = null;
        } catch (e: any) {
            error = e.message;
        }
    }
    loadEntities();
</script>

<div class="grid md:grid-cols-2 gap-6">
    <div class="grid gap-4 content-start">
        <div class="flex gap-2 items-center flex-wrap">
            <button
                class="btn btn-primary"
                on:click={createEntity}
                disabled={creating}
                >{creating ? "Creating..." : "Create Entity"}</button
            >
            <button class="btn" on:click={loadEntities} disabled={loading}
                >Refresh</button
            >
            {#if selectedIdGen}<button class="btn" on:click={delEntity}
                    >Delete Selected</button
                >{/if}
        </div>

        <!-- Auto-reload controls - separate row for better visibility -->
        <div
            class="flex gap-3 items-center p-3 bg-zinc-800/50 rounded border border-zinc-700"
        >
            <label class="flex items-center gap-2 cursor-pointer">
                <input
                    type="checkbox"
                    bind:checked={autoReloadEnabled}
                    class="w-4 h-4 accent-emerald-500"
                />
                <span class="text-sm font-medium">Auto-reload</span>
            </label>
            {#if autoReloadEnabled}
                <select
                    bind:value={autoReloadInterval}
                    class="bg-zinc-900 border border-zinc-700 rounded px-2 py-1 text-xs focus:outline-none focus:ring-2 focus:ring-emerald-500/50 focus:border-emerald-500"
                >
                    {#each intervalOptions as option}
                        <option value={option.value}>{option.label}</option>
                    {/each}
                </select>
                <span
                    class="text-xs text-emerald-400"
                    title={autoReloadTimer
                        ? "Auto-reload is active"
                        : "Auto-reload is paused"}
                >
                </span>
            {/if}
        </div>
        {#if error}<p class="text-red-400 text-sm">{error}</p>{/if}
        <EntitiesTable {entities} {loading} onSelect={select} />
    </div>
    <div class="grid gap-4 content-start">
        <EntityDetailPanel
            {detail}
            onClose={() => {
                selectedIdGen = null;
                detail = null;
                // Reset edit mode when closing
                editMode = false;
                editComponentType = "";
                editComponentData = null;
            }}
            onRemoveComponent={removeComponent}
            onUpdateComponent={updateComponent}
        >
            {#if detail}
                <AddComponentForm
                    onSubmit={applyComponent}
                    {editMode}
                    {editComponentType}
                    {editComponentData}
                />
                <div class="text-[10px] text-zinc-500 mt-1">Auto-refreshed at {new Date().toLocaleTimeString()}</div>
            {/if}
        </EntityDetailPanel>
    </div>
</div>
