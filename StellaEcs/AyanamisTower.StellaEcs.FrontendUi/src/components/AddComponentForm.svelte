<script lang="ts">
    import { api, type ComponentInfo } from "@lib/api";
    export let onSubmit: (type: string, data: any) => Promise<void> | void;
    export let editMode: boolean = false;
    export let editComponentType: string = "";
    export let editComponentData: any = null;

    let availableComponents: ComponentInfo[] = [];
    let selectedType = "";
    let componentData: Record<string, any> = {};
    let jsonMode = false;
    let jsonText = "{}";
    let sending = false;
    let loading = false;
    let error: string | null = null;
    let editDataPopulated = false;

    // Watch for edit mode changes - only populate once per edit session
    $: if (
        editMode &&
        editComponentType &&
        editComponentData &&
        !editDataPopulated
    ) {
        populateEditData();
        editDataPopulated = true;
    }

    // Reset the flag when edit mode changes
    $: if (!editMode) {
        editDataPopulated = false;
    }

    function populateEditData() {
        selectedType = editComponentType;

        // Extract the actual data from the component data structure
        let dataToEdit = editComponentData;
        if (
            dataToEdit &&
            typeof dataToEdit === "object" &&
            "value" in dataToEdit
        ) {
            dataToEdit = dataToEdit.value;
        }

        componentData = dataToEdit ? { ...dataToEdit } : {};
        jsonText = JSON.stringify(componentData, null, 2);
    }

    function resetForm() {
        selectedType =
            availableComponents.length > 0
                ? availableComponents[0].typeName
                : "";
        componentData = {};
        jsonText = "{}";
        if (selectedType) {
            generateDefaultFields();
        }
    }

    // Load available component types
    async function loadComponentTypes() {
        loading = true;
        try {
            availableComponents = await api.components();
            if (!editMode && availableComponents.length > 0 && !selectedType) {
                selectedType = availableComponents[0].typeName;
                onTypeChange();
            }
        } catch (e: any) {
            error = `Failed to load component types: ${e.message}`;
        } finally {
            loading = false;
        }
    }

    // Generate form fields based on component type
    function onTypeChange() {
        if (!selectedType) return;

        // Reset data
        componentData = {};
        jsonText = "{}";

        // Find the selected component info
        const componentInfo = availableComponents.find(
            (c) => c.typeName === selectedType,
        );
        if (componentInfo?.data) {
            // If we have example data, use it as a template
            componentData = { ...componentInfo.data };
            jsonText = JSON.stringify(componentData, null, 2);
        } else {
            // Create default fields based on common component patterns
            generateDefaultFields();
        }
    }

    function generateDefaultFields() {
        // Generate reasonable defaults based on component name
        const typeName = selectedType.toLowerCase();

        if (typeName.includes("position")) {
            componentData = { x: 0, y: 0 };
        } else if (typeName.includes("velocity")) {
            componentData = { x: 0, y: 0 };
        } else if (typeName.includes("health")) {
            componentData = { current: 100, max: 100 };
        } else if (typeName.includes("name")) {
            componentData = { value: "" };
        } else if (typeName.includes("transform")) {
            componentData = { x: 0, y: 0, rotation: 0, scale: 1 };
        } else {
            componentData = {};
        }

        jsonText = JSON.stringify(componentData, null, 2);
    }

    function updateJsonFromForm() {
        try {
            jsonText = JSON.stringify(componentData, null, 2);
        } catch (e) {
            // If there's an error, don't update
        }
    }

    function updateFormFromJson() {
        try {
            componentData = JSON.parse(jsonText);
            error = null;
        } catch (e: any) {
            error = `Invalid JSON: ${e.message}`;
        }
    }

    async function submit() {
        error = null;
        sending = true;
        try {
            let dataToSend = jsonMode ? JSON.parse(jsonText) : componentData;
            await onSubmit(selectedType, dataToSend);

            // Reset form only if not in edit mode
            if (!editMode) {
                resetForm();
            }
        } catch (e: any) {
            error = e.message;
        } finally {
            sending = false;
        }
    }

    function cancelEdit() {
        // Reset to non-edit mode
        editMode = false;
        editComponentType = "";
        editComponentData = null;
        editDataPopulated = false;
        resetForm();
    }

    // Initialize
    loadComponentTypes();
</script>

<form class="grid gap-3" on:submit|preventDefault={submit}>
    <div class="flex items-center justify-between">
        <h4 class="text-sm font-semibold">
            {editMode ? `Edit ${editComponentType}` : "Add / Update Component"}
        </h4>
        {#if editMode}
            <button type="button" class="btn text-xs" on:click={cancelEdit}>
                Cancel Edit
            </button>
        {/if}
    </div>

    {#if loading}
        <p class="text-zinc-500 text-sm">Loading component types...</p>
    {:else if availableComponents.length === 0}
        <p class="text-zinc-500 text-sm">No component types available</p>
    {:else}
        <!-- Component Type Selection -->
        <div class="grid gap-2">
            <label class="text-sm font-medium">Component Type</label>
            <select
                class="input"
                bind:value={selectedType}
                on:change={onTypeChange}
                disabled={editMode}
                required
            >
                {#each availableComponents as component}
                    <option value={component.typeName}>
                        {component.typeName}
                        {#if component.pluginOwner}
                            ({component.pluginOwner}){/if}
                    </option>
                {/each}
            </select>
        </div>

        <!-- Mode Toggle -->
        <div class="flex gap-2 items-center">
            <label class="text-sm font-medium">Edit Mode:</label>
            <button
                type="button"
                class="btn {!jsonMode ? 'btn-primary' : ''}"
                on:click={() => {
                    jsonMode = false;
                    updateJsonFromForm();
                }}
            >
                Form
            </button>
            <button
                type="button"
                class="btn {jsonMode ? 'btn-primary' : ''}"
                on:click={() => {
                    jsonMode = true;
                    updateFormFromJson();
                }}
            >
                JSON
            </button>
        </div>

        {#if jsonMode}
            <!-- JSON Editor Mode -->
            <div class="grid gap-2">
                <label class="text-sm font-medium">Component Data (JSON)</label>
                <textarea
                    class="input font-mono text-sm"
                    rows="8"
                    bind:value={jsonText}
                    on:input={updateFormFromJson}
                    placeholder="Enter component data as JSON"
                ></textarea>
            </div>
        {:else}
            <!-- Form Mode -->
            <div class="grid gap-2">
                <label class="text-sm font-medium">Component Data</label>
                {#if Object.keys(componentData).length === 0}
                    <p class="text-zinc-500 text-sm">
                        This component has no configurable fields, or switch to
                        JSON mode to add custom data.
                    </p>
                {:else}
                    {#each Object.entries(componentData) as [key, value]}
                        <div class="flex gap-2 items-center">
                            <label class="text-sm w-20">{key}:</label>
                            {#if typeof value === "number"}
                                <input
                                    type="number"
                                    class="input flex-1"
                                    bind:value={componentData[key]}
                                    on:input={updateJsonFromForm}
                                />
                            {:else if typeof value === "boolean"}
                                <label class="flex items-center gap-2 flex-1">
                                    <input
                                        type="checkbox"
                                        bind:checked={componentData[key]}
                                        on:change={updateJsonFromForm}
                                    />
                                    <span class="text-sm"
                                        >{componentData[key]
                                            ? "True"
                                            : "False"}</span
                                    >
                                </label>
                            {:else}
                                <input
                                    type="text"
                                    class="input flex-1"
                                    bind:value={componentData[key]}
                                    on:input={updateJsonFromForm}
                                />
                            {/if}
                        </div>
                    {/each}
                {/if}
            </div>
        {/if}

        <!-- Preview -->
        {#if !jsonMode && Object.keys(componentData).length > 0}
            <div class="grid gap-1">
                <label class="text-sm font-medium">Preview (JSON):</label>
                <pre
                    class="bg-zinc-800 p-2 rounded text-xs overflow-auto">{jsonText}</pre>
            </div>
        {/if}

        {#if error}
            <p class="text-red-400 text-xs">{error}</p>
        {/if}

        <button
            class="btn btn-primary"
            disabled={sending || !selectedType}
            type="submit"
        >
            {#if sending}
                {editMode ? "Updating..." : "Adding..."}
            {:else}
                {editMode ? "Update Component" : "Add Component"}
            {/if}
        </button>
    {/if}
</form>
