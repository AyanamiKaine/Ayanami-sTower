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
    // Track expanded/collapsed state for large vectors/arrays per component+key
    let expandedVectors: Record<string, Record<string, boolean>> = {};

    function toggleExpanded(componentType: string, key: string) {
        if (!expandedVectors[componentType]) expandedVectors[componentType] = {};
        expandedVectors[componentType][key] = !expandedVectors[componentType][key];
        expandedVectors = expandedVectors; // trigger reactivity
    }

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

    function buildDisplayData(data: any): any {
        if (!data || typeof data !== "object") return data;
        const hasValue = Object.prototype.hasOwnProperty.call(data, "value");
        const val = (data as any).value;
        // If value is an object, merge it with sibling keys so nothing gets lost (e.g., Position3D)
        if (hasValue && val && typeof val === "object") {
            const siblings: Record<string, any> = {};
            for (const [k, v] of Object.entries(data)) {
                if (k !== "value") siblings[k] = v;
            }
            return { ...val, ...siblings };
        }
        // If only 'value' exists and it's primitive, unwrap it
        if (hasValue && Object.keys(data).length === 1) {
            return val;
        }
        // Otherwise, keep as-is
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

            // Deep-clone the original data so we can set nested keys safely
            const original = component.data as any;
            let dataToSend: any;
            if (original && typeof original === 'object') {
                try {
                    dataToSend = JSON.parse(JSON.stringify(original));
                } catch (err) {
                    // Fallback to shallow clone if structured clone fails
                    dataToSend = { ...(original ?? {}) };
                }
            } else {
                // If original is primitive or missing, start with an object so nested sets work
                dataToSend = {};
            }

            // Support dot-path keys for nested fields (e.g. "Direction.X" or "Direction.x")
            function setDeep(obj: any, path: string[], value: any) {
                let cur = obj;
                for (let i = 0; i < path.length - 1; i++) {
                    const k = path[i];
                    const next = path[i + 1];
                    const nextIsIndex = Number.isInteger(Number(next));
                    if (cur[k] === undefined || cur[k] === null || typeof cur[k] !== 'object') {
                        // Create array if next segment looks like an index
                        cur[k] = nextIsIndex ? [] : {};
                    }
                    // If existing value is object but not the desired container, convert
                    if (nextIsIndex && !Array.isArray(cur[k])) {
                        cur[k] = [];
                    }
                    if (!nextIsIndex && Array.isArray(cur[k])) {
                        cur[k] = { ...cur[k] };
                    }
                    cur = cur[k];
                }
                const last = path[path.length - 1];
                // If last is numeric index and container is array, set at index
                if (Array.isArray(cur) && Number.isInteger(Number(last))) {
                    cur[Number(last)] = value;
                } else {
                    cur[last] = value;
                }
            }

            const path = String(key).split('.');
            setDeep(dataToSend, path, newValue);

            console.log(`Updating component ${componentType}.${key} = ${newValue}`);
            console.log('Original component data:', component.data);
            console.log('Data being sent to backend:', dataToSend);
            await onUpdateComponent(componentType, dataToSend);

            // Save the editing value so UI reflects what was saved
            editingValues[componentType][key] = newValue;
        } catch (error) {
            console.error("Failed to update component:", error);
            // Revert the editing value on error
            const component = detail.components.find(
                (c) => c.typeName === componentType,
            );
            if (component) {
                const displayMerged = buildDisplayData(component.data);
                if (displayMerged && typeof displayMerged === "object") {
                    // If this was a nested key path, try to read the nested value
                    if (key.includes('.')) {
                        const parts = key.split('.');
                        let cur: any = displayMerged;
                        for (const p of parts) {
                            if (cur && typeof cur === 'object') cur = cur[p];
                            else { cur = undefined; break; }
                        }
                        editingValues[componentType][key] = cur;
                    } else {
                        editingValues[componentType][key] = (displayMerged as any)[key];
                    }
                } else {
                    editingValues[componentType][key] = displayMerged;
                }
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
        // For numbers, keep the raw string so editing (like "1.") isn't coerced away.
        // Parse a numeric value for sending when it represents a valid number.
        let raw: any = target.value;
        let sendValue: any = raw;
        let isNumber = false;

        if (target.type === 'number') {
            isNumber = true;
            raw = target.value; // keep the raw string
            // Accept comma as decimal separator when parsing for send
            const normalized = raw.replace(',', '.');
            const parsed = Number(normalized);
            sendValue = { raw, parsed, isNumber: true };
        } else if (target.type === 'checkbox') {
            raw = target.checked;
            sendValue = raw;
        } else {
            raw = target.value;
            sendValue = raw;
        }

        // Ensure the componentType exists in editingValues
        if (!editingValues[componentType]) {
            editingValues[componentType] = {};
        }

        // Store raw for display/editing. For non-number, raw is already the typed value/boolean.
        editingValues[componentType][key] = raw;

        // Debounce the update; pass the sendValue wrapper for number inputs so debounce can decide.
        debounceUpdate(componentType, key, sendValue);
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
            // If this was a numeric input, value is an object { raw, parsed, isNumber }
            if (value && typeof value === 'object' && value.isNumber) {
                const raw: string = String(value.raw ?? '');
                const parsed: number = value.parsed;

                // Don't send updates for incomplete numeric input states like empty string,
                // a lone '-' or a trailing decimal point (e.g., '1.'), or a NaN parse.
                const lastChar = raw.length > 0 ? raw[raw.length - 1] : '';
                const isIncomplete = raw === '' || raw === '-' || lastChar === '.' || lastChar === ',';
                if (!isIncomplete && Number.isFinite(parsed)) {
                    updateComponentValue(componentType, key, parsed);
                } else {
                    // Skip sending; keep the raw editing value in the UI only.
                }
            } else {
                // Non-number: send value directly
                updateComponentValue(componentType, key, value);
            }

            delete updateTimeouts[timeoutKey];
        }, 1000); // 500ms debounce
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
                {@const displayValue = buildDisplayData(c.data)}
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
                                                    {#if Array.isArray(value)}
                                                        {#if value.length > 10 && !(expandedVectors[c.typeName]?.[key])}
                                                            <div class="flex items-center justify-between gap-2">
                                                                <div class="text-xs text-zinc-400">Array({value.length})</div>
                                                                <button class="btn text-xs" on:click={() => toggleExpanded(c.typeName, key)}>Show all</button>
                                                            </div>
                                                            <ul class="pl-4 text-xs mt-2 space-y-1">
                                                                {#each value.slice(0, 10) as item, idx}
                                                                    <li class="font-mono">{idx}: {formatComponentValue(item)}</li>
                                                                {/each}
                                                            </ul>
                                                        {:else}
                                                            <div class="grid gap-1 w-full">
                                                                {#each value as subValue, idx}
                                                                    <div class="flex gap-2 items-start bg-zinc-800/40 p-2 rounded">
                                                                        <span class="text-zinc-400 w-16 text-xs font-medium">{idx}:</span>
                                                                        <span class="text-xs">{getValueTypeIcon(subValue)}</span>
                                                                        <div class="flex-1">
                                                                            {#if typeof subValue === 'number'}
                                                                                <input
                                                                                    type="number"
                                                                                    class="input text-xs w-20"
                                                                                    value={editingValues[c.typeName]?.[`${key}.${idx}`] ?? subValue}
                                                                                    on:input={(e) => handleInputChange(c.typeName, `${key}.${idx}`, e)}
                                                                                    step="any"
                                                                                />
                                                                            {:else if typeof subValue === 'boolean'}
                                                                                <label class="flex items-center gap-1">
                                                                                    <input
                                                                                        type="checkbox"
                                                                                        checked={editingValues[c.typeName]?.[`${key}.${idx}`] ?? subValue}
                                                                                        on:change={(e) => handleInputChange(c.typeName, `${key}.${idx}`, e)}
                                                                                    />
                                                                                    <span class="text-xs">{(editingValues[c.typeName]?.[`${key}.${idx}`] ?? subValue) ? 'True' : 'False'}</span>
                                                                                </label>
                                                                            {:else if typeof subValue === 'string'}
                                                                                <input
                                                                                    type="text"
                                                                                    class="input text-xs flex-1"
                                                                                    value={editingValues[c.typeName]?.[`${key}.${idx}`] ?? subValue}
                                                                                    on:input={(e) => handleInputChange(c.typeName, `${key}.${idx}`, e)}
                                                                                />
                                                                            {:else if isExpandableObject(subValue)}
                                                                                <div class="grid gap-1">
                                                                                    {#each Object.entries(subValue) as [innerKey, innerVal]}
                                                                                        <div class="flex gap-2 items-center bg-zinc-800/30 p-2 rounded">
                                                                                            <span class="text-zinc-400 w-16 text-xs font-medium">{innerKey}:</span>
                                                                                            <span class="text-xs">{getValueTypeIcon(innerVal)}</span>
                                                                                            <div class="flex-1">
                                                                                                {#if typeof innerVal === 'number'}
                                                                                                    <input type="number" class="input text-xs w-20" value={editingValues[c.typeName]?.[`${key}.${idx}.${innerKey}`] ?? innerVal} on:input={(e) => handleInputChange(c.typeName, `${key}.${idx}.${innerKey}`, e)} step="any" />
                                                                                                {:else if typeof innerVal === 'boolean'}
                                                                                                    <label class="flex items-center gap-1"><input type="checkbox" checked={editingValues[c.typeName]?.[`${key}.${idx}.${innerKey}`] ?? innerVal} on:change={(e) => handleInputChange(c.typeName, `${key}.${idx}.${innerKey}`, e)} /><span class="text-xs">{(editingValues[c.typeName]?.[`${key}.${idx}.${innerKey}`] ?? innerVal) ? 'True' : 'False'}</span></label>
                                                                                                {:else if typeof innerVal === 'string'}
                                                                                                    <input type="text" class="input text-xs flex-1" value={editingValues[c.typeName]?.[`${key}.${idx}.${innerKey}`] ?? innerVal} on:input={(e) => handleInputChange(c.typeName, `${key}.${idx}.${innerKey}`, e)} />
                                                                                                {:else}
                                                                                                    <pre class="text-xs font-mono">{formatComponentValue(innerVal)}</pre>
                                                                                                {/if}
                                                                                            </div>
                                                                                        </div>
                                                                                    {/each}
                                                                                </div>
                                                                            {:else}
                                                                                <span class="text-emerald-400 text-xs font-mono">{formatComponentValue(subValue)}</span>
                                                                            {/if}
                                                                        </div>
                                                                    </div>
                                                                {/each}
                                                                {#if value.length > 10}
                                                                    <div class="text-right">
                                                                        <button class="btn text-xs" on:click={() => toggleExpanded(c.typeName, key)}>Hide</button>
                                                                    </div>
                                                                {/if}
                                                            </div>
                                                        {/if}
                                                    {:else}
                                                        <div class="grid gap-1 w-full">
                                                            {#each Object.entries(value) as [subKey, subValue]}
                                                                <div class="flex gap-2 items-center bg-zinc-800/40 p-2 rounded">
                                                                    <span class="text-zinc-400 w-16 text-xs font-medium">{subKey}:</span>
                                                                    <span class="text-xs">{getValueTypeIcon(subValue)}</span>
                                                                    <div class="flex-1">
                                                                        {#if typeof subValue === 'number'}
                                                                            <input
                                                                                type="number"
                                                                                class="input text-xs w-20"
                                                                                value={editingValues[c.typeName]?.[`${key}.${subKey}`] ?? subValue}
                                                                                on:input={(e) => handleInputChange(c.typeName, `${key}.${subKey}`, e)}
                                                                                step="any"
                                                                            />
                                                                        {:else if typeof subValue === 'boolean'}
                                                                            <label class="flex items-center gap-1">
                                                                                <input
                                                                                    type="checkbox"
                                                                                    checked={editingValues[c.typeName]?.[`${key}.${subKey}`] ?? subValue}
                                                                                    on:change={(e) => handleInputChange(c.typeName, `${key}.${subKey}`, e)}
                                                                                />
                                                                                <span class="text-xs">{(editingValues[c.typeName]?.[`${key}.${subKey}`] ?? subValue) ? 'True' : 'False'}</span>
                                                                            </label>
                                                                        {:else if typeof subValue === 'string'}
                                                                            <input
                                                                                type="text"
                                                                                class="input text-xs flex-1"
                                                                                value={editingValues[c.typeName]?.[`${key}.${subKey}`] ?? subValue}
                                                                                on:input={(e) => handleInputChange(c.typeName, `${key}.${subKey}`, e)}
                                                                            />
                                                                        {:else if isExpandableObject(subValue)}
                                                                            <pre class="text-xs font-mono">{JSON.stringify(subValue, null, 2)}</pre>
                                                                        {:else}
                                                                            <span class="text-emerald-400 text-xs font-mono">{formatComponentValue(subValue)}</span>
                                                                        {/if}
                                                                    </div>
                                                                </div>
                                                            {/each}
                                                        </div>
                                                    {/if}
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
