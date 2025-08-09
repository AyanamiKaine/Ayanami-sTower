<script lang="ts">
    import { api, type EntityDetail } from "@lib/api";
    export let detail: EntityDetail | null = null;
    export let onClose: () => void;
    export let onRemoveComponent: (type: string) => void;
    export let onUpdateComponent:
        | ((type: string, newData: any) => void)
        | undefined = undefined;

    let editingValues: Record<string, Record<string, any>> = {};
    let updatingComponents: Set<string> = new Set();
    let currentEntityId: string | null = null;
    let isInteracting = false; // true while an input inside the panel has focus

    // Keep UI in sync with latest server data on any detail refresh.
    // Do NOT pre-populate editingValues from server; only store user edits.
    // This ensures auto-reload shows fresh values unless the user is actively editing.
    $: if (detail) {
        const entityId = `${detail.id}`;
        if (currentEntityId !== entityId) {
            // Switched entities: always clear local edits
            currentEntityId = entityId;
            editingValues = {};
        } else if (!isInteracting) {
            // Same entity refreshed: clear only if not actively editing
            editingValues = {};
        }
    }

    function formatComponentValue(value: any): string {
        if (value === null || value === undefined) {
            return "null";
        }
        if (typeof value === "object") {
            return JSON.stringify(value, null, 2);
        }
        return String(value);
    }

    function getDisplayValue(data: any): any {
        // If data has a 'value' property, use that (common pattern)
        if (data && typeof data === "object" && "value" in data) {
            return data.value;
        }
        // Otherwise return the data as-is
        return data;
    }

    function isExpandableObject(value: any): boolean {
        return (
            value && typeof value === "object" && Object.keys(value).length > 0
        );
    }

    function getValueTypeIcon(value: any): string {
        if (typeof value === "number") return "🔢";
        if (typeof value === "boolean") return value ? "✅" : "❌";
        if (typeof value === "string") return "📝";
        if (value === null || value === undefined) return "∅";
        if (typeof value === "object") return "📦";
        return "❓";
    }

    async function updateComponentValue(
        componentType: string,
        key: string,
        newValue: any,
    ) {
        if (!onUpdateComponent || !detail) return;

        updatingComponents.add(componentType);
        updatingComponents = updatingComponents; // Trigger reactivity

        try {
            // Find the component
            const component = detail.components.find(
                (c) => c.typeName === componentType,
            );
            if (!component) return;

            const displayValue = getDisplayValue(component.data);

            // Create updated display data
            const updatedDisplayData = { ...displayValue, [key]: newValue };

            // Reconstruct the data in the original structure expected by backend
            let dataToSend;
            if (
                component.data &&
                typeof component.data === "object" &&
                "value" in component.data
            ) {
                // If original data had a 'value' wrapper, reconstruct it
                if (key === "value") {
                    // For single-value updates, replace the entire value
                    dataToSend = { ...component.data, value: newValue };
                } else {
                    // For multi-property updates, update the value object
                    dataToSend = {
                        ...component.data,
                        value: updatedDisplayData,
                    };
                }
            } else {
                // Otherwise send the updated data directly
                dataToSend = updatedDisplayData;
            }

            // Call the update function
            console.log(
                `Updating component ${componentType}.${key} = ${newValue}`,
            );
            console.log("Original component data:", component.data);
            console.log("Data being sent to backend:", dataToSend);
            await onUpdateComponent(componentType, dataToSend);

            // Update the editing value to match what was saved
            editingValues[componentType][key] = newValue;
        } catch (error) {
            console.error("Failed to update component:", error);
            // Revert the editing value on error
            const component = detail.components.find(
                (c) => c.typeName === componentType,
            );
            if (component) {
                const displayValue = getDisplayValue(component.data);
                editingValues[componentType][key] = displayValue[key];
            }
        } finally {
            updatingComponents.delete(componentType);
            updatingComponents = updatingComponents; // Trigger reactivity
        }
    }

    function handleInputChange(
        componentType: string,
        key: string,
        event: Event,
    ) {
        const target = event.target as HTMLInputElement;
        const value =
            target.type === "number"
                ? parseFloat(target.value) || 0
                : target.type === "checkbox"
                  ? target.checked
                  : target.value;

        // Ensure the componentType exists in editingValues
        if (!editingValues[componentType]) {
            editingValues[componentType] = {};
        }

        editingValues[componentType][key] = value;

        // Debounce the update
        debounceUpdate(componentType, key, value);
    }

    let updateTimeouts: Record<string, any> = {};

    function debounceUpdate(componentType: string, key: string, value: any) {
        const timeoutKey = `${componentType}-${key}`;

        // Clear existing timeout
        if (updateTimeouts[timeoutKey]) {
            clearTimeout(updateTimeouts[timeoutKey]);
        }

        // Set new timeout
        updateTimeouts[timeoutKey] = setTimeout(() => {
            updateComponentValue(componentType, key, value);
            delete updateTimeouts[timeoutKey];
        }, 500); // 500ms debounce
    }
</script>

{#if detail}
    <div class="card fade-in grid gap-4" on:focusin={() => (isInteracting = true)} on:focusout={() => (isInteracting = false)}>
        <div class="flex items-center gap-3">
            <h3 class="font-semibold">
                Entity {detail.id}
            </h3>
            <button class="ml-auto btn" on:click={onClose}>Close</button>
        </div>
        <h4 class="text-sm font-semibold">Components</h4>
        <div class="grid gap-3 text-sm">
            {#each detail.components as c}
                {@const displayValue = getDisplayValue(c.data)}
                <div
                    class="p-3 rounded border border-zinc-700 bg-zinc-900/30 hover:border-zinc-600 transition-colors"
                >
                    <div class="flex items-center justify-between mb-2">
                        <div class="flex items-center gap-2">
                            <span class="font-medium text-base"
                                >{c.typeName}</span
                            >
                            {#if c.pluginOwner}
                                <span class="badge">{c.pluginOwner}</span>
                            {/if}
                            {#if updatingComponents.has(c.typeName)}
                                <span class="text-xs text-yellow-400"
                                    >Updating...</span
                                >
                            {/if}
                        </div>
                        <button
                            class="btn text-xs"
                            on:click={() => onRemoveComponent(c.typeName)}
                            title="Remove component"
                        >
                            Remove
                        </button>
                    </div>

                    {#if c.data}
                        {#if displayValue && isExpandableObject(displayValue)}
                            <div class="grid gap-2 text-sm">
                                <div class="text-zinc-400 text-xs font-medium">
                                    Properties:
                                </div>
                                <div class="grid gap-2 pl-2">
                                    {#each Object.entries(displayValue) as [key, value]}
                                        <div
                                            class="flex gap-2 items-center bg-zinc-800/40 p-2 rounded"
                                        >
                                            <span
                                                class="text-zinc-400 w-16 text-xs font-medium"
                                                >{key}:</span
                                            >
                                            <span class="text-xs"
                                                >{getValueTypeIcon(value)}</span
                                            >
                                            <div class="flex-1">
                                                {#if typeof value === "number"}
                                                    <input
                                                        type="number"
                                                        class="input text-xs w-20"
                                                        value={editingValues[
                                                            c.typeName
                                                        ]?.[key] ?? value}
                                                        on:input={(e) =>
                                                            handleInputChange(
                                                                c.typeName,
                                                                key,
                                                                e,
                                                            )}
                                                        step="any"
                                                    />
                                                {:else if typeof value === "boolean"}
                                                    <label
                                                        class="flex items-center gap-1"
                                                    >
                                                        <input
                                                            type="checkbox"
                                                            checked={editingValues[
                                                                c.typeName
                                                            ]?.[key] ?? value}
                                                            on:change={(e) =>
                                                                handleInputChange(
                                                                    c.typeName,
                                                                    key,
                                                                    e,
                                                                )}
                                                        />
                                                        <span class="text-xs"
                                                            >{(editingValues[
                                                                c.typeName
                                                            ]?.[key] ?? value)
                                                                ? "True"
                                                                : "False"}</span
                                                        >
                                                    </label>
                                                {:else if typeof value === "string"}
                                                    <input
                                                        type="text"
                                                        class="input text-xs flex-1"
                                                        value={editingValues[
                                                            c.typeName
                                                        ]?.[key] ?? value}
                                                        on:input={(e) =>
                                                            handleInputChange(
                                                                c.typeName,
                                                                key,
                                                                e,
                                                            )}
                                                    />
                                                {:else if isExpandableObject(value)}
                                                    <pre
                                                        class="text-xs bg-zinc-800 p-2 rounded overflow-auto flex-1 max-h-32">{formatComponentValue(
                                                            value,
                                                        )}</pre>
                                                {:else}
                                                    <span
                                                        class="text-emerald-400 text-xs font-mono"
                                                    >
                                                        {formatComponentValue(
                                                            value,
                                                        )}
                                                    </span>
                                                {/if}
                                            </div>
                                        </div>
                                    {/each}
                                </div>
                            </div>
                        {:else if displayValue !== null && displayValue !== undefined}
                            <div class="bg-zinc-800/40 p-2 rounded">
                                <div class="flex items-center gap-2">
                                    <span class="text-xs"
                                        >{getValueTypeIcon(displayValue)}</span
                                    >
                                    <span class="text-zinc-400 text-xs"
                                        >Value:
                                    </span>
                                    {#if typeof displayValue === "number"}
                                        <input
                                            type="number"
                                            class="input text-xs w-20"
                                            value={editingValues[c.typeName]
                                                ?.value ?? displayValue}
                                            on:input={(e) =>
                                                handleInputChange(
                                                    c.typeName,
                                                    "value",
                                                    e,
                                                )}
                                            step="any"
                                        />
                                    {:else if typeof displayValue === "boolean"}
                                        <label class="flex items-center gap-1">
                                            <input
                                                type="checkbox"
                                                checked={editingValues[
                                                    c.typeName
                                                ]?.value ?? displayValue}
                                                on:change={(e) =>
                                                    handleInputChange(
                                                        c.typeName,
                                                        "value",
                                                        e,
                                                    )}
                                            />
                                            <span class="text-xs"
                                                >{(editingValues[c.typeName]
                                                    ?.value ?? displayValue)
                                                    ? "True"
                                                    : "False"}</span
                                            >
                                        </label>
                                    {:else if typeof displayValue === "string"}
                                        <input
                                            type="text"
                                            class="input text-xs flex-1"
                                            value={editingValues[c.typeName]
                                                ?.value ?? displayValue}
                                            on:input={(e) =>
                                                handleInputChange(
                                                    c.typeName,
                                                    "value",
                                                    e,
                                                )}
                                        />
                                    {:else}
                                        <span
                                            class="text-emerald-400 font-mono text-xs"
                                        >
                                            {formatComponentValue(displayValue)}
                                        </span>
                                    {/if}
                                </div>
                            </div>
                        {:else}
                            <div
                                class="text-zinc-500 text-xs italic bg-zinc-800/20 p-2 rounded"
                            >
                                No data
                            </div>
                        {/if}
                    {:else}
                        <div
                            class="text-zinc-500 text-xs italic bg-zinc-800/20 p-2 rounded"
                        >
                            No data
                        </div>
                    {/if}
                </div>
            {/each}
            {#if detail.components.length === 0}
                <div
                    class="text-zinc-500 text-center py-6 border border-dashed border-zinc-700 rounded"
                >
                    No components attached.
                </div>
            {/if}
        </div>
        <slot />
    </div>
{/if}
