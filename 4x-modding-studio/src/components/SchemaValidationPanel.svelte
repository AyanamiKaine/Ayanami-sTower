<script lang="ts">
    import { untrack } from "svelte";
    import type { XMLNode } from "../lib/xml/parser";
    import {
        createX4SchemaManager,
        validateXMLNode,
        type SchemaManager,
        type ValidationError,
        type ValidationResult,
        type XMLValidationNode,
    } from "../lib/xml/schema-validator";
    import type { SchemaResponse, SchemaInfo } from "../pages/api/schema";

    // Props
    interface Props {
        rootNode: XMLNode | null;
        x4DataPath: string;
    }

    let { rootNode, x4DataPath }: Props = $props();

    // State
    let schemaManager = $state<SchemaManager>(createX4SchemaManager());
    let loadedSchemas = $state<string[]>(["_builtin_"]);
    let availableSchemas = $state<SchemaInfo[]>([]);
    let loadingSchemas = $state(false);
    let schemaLoadError = $state<string | null>(null);
    let validationResult = $state<ValidationResult | null>(null);
    let isValidating = $state(false);
    let showErrors = $state(true);
    let showWarnings = $state(true);
    let autoValidate = $state(true);
    let panelCollapsed = $state(true);

    // Fetch available schemas when x4DataPath changes and auto-load all
    $effect(() => {
        if (x4DataPath) {
            fetchAndLoadAllSchemas(x4DataPath);
        }
    });

    // Auto-validate when rootNode changes
    $effect(() => {
        if (rootNode && autoValidate) {
            // Use untrack to prevent infinite loops
            untrack(() => {
                validateDocument();
            });
        }
    });

    async function fetchAndLoadAllSchemas(basePath: string) {
        if (!basePath) return;

        loadingSchemas = true;
        schemaLoadError = null;

        try {
            const response = await fetch(
                `/api/schema?action=list&basePath=${encodeURIComponent(basePath)}`,
            );
            const data: SchemaResponse = await response.json();

            if (data.success && data.schemas) {
                availableSchemas = data.schemas;
                // Auto-load all schemas
                for (const schema of data.schemas) {
                    if (!loadedSchemas.includes(schema.name)) {
                        await loadSchemaQuiet(schema.name);
                    }
                }
            } else {
                schemaLoadError = data.error || "Failed to list schemas";
            }
        } catch (e) {
            schemaLoadError =
                e instanceof Error ? e.message : "Failed to fetch schemas";
        } finally {
            loadingSchemas = false;
            // Re-validate after loading schemas
            if (rootNode && autoValidate) {
                validateDocument();
            }
        }
    }

    // Load schema without setting loading state (for batch loading)
    async function loadSchemaQuiet(schemaName: string) {
        if (!x4DataPath || loadedSchemas.includes(schemaName)) return;

        try {
            const response = await fetch(
                `/api/schema?action=read&basePath=${encodeURIComponent(x4DataPath)}&schema=${encodeURIComponent(schemaName)}`,
            );
            const data: SchemaResponse = await response.json();

            if (data.success && data.content) {
                schemaManager.loadSchema(schemaName, data.content);
                loadedSchemas = [...loadedSchemas, schemaName];
            }
        } catch (e) {
            console.error(`Failed to load schema ${schemaName}:`, e);
        }
    }

    async function loadSchema(schemaName: string) {
        if (!x4DataPath || loadedSchemas.includes(schemaName)) return;

        loadingSchemas = true;
        schemaLoadError = null;

        try {
            const response = await fetch(
                `/api/schema?action=read&basePath=${encodeURIComponent(x4DataPath)}&schema=${encodeURIComponent(schemaName)}`,
            );
            const data: SchemaResponse = await response.json();

            if (data.success && data.content) {
                schemaManager.loadSchema(schemaName, data.content);
                loadedSchemas = [...loadedSchemas, schemaName];

                // Re-validate if we have a document
                if (rootNode && autoValidate) {
                    validateDocument();
                }
            } else {
                schemaLoadError = data.error || "Failed to load schema";
            }
        } catch (e) {
            schemaLoadError =
                e instanceof Error ? e.message : "Failed to load schema";
        } finally {
            loadingSchemas = false;
        }
    }

    function convertToValidationNode(
        node: XMLNode,
        path = "",
    ): XMLValidationNode {
        const nodePath = path ? `${path}/${node.name}` : `/${node.name}`;

        return {
            name: node.name,
            attributes: { ...node.attributes },
            children: node.children
                .filter((c) => c.type === "element")
                .map((child, idx) => {
                    const siblingsBefore = node.children
                        .slice(0, node.children.indexOf(child))
                        .filter(
                            (c) =>
                                c.type === "element" && c.name === child.name,
                        ).length;
                    const indexSuffix =
                        siblingsBefore > 0 ? `[${siblingsBefore + 1}]` : "";
                    return convertToValidationNode(
                        child,
                        `${nodePath}${indexSuffix}`,
                    );
                }),
            textContent: node.textContent,
            path: nodePath,
        };
    }

    function validateDocument() {
        if (!rootNode) {
            validationResult = null;
            return;
        }

        isValidating = true;

        try {
            const validationNode = convertToValidationNode(rootNode);
            validationResult = validateXMLNode(validationNode, schemaManager);
        } catch (e) {
            console.error("Validation error:", e);
            validationResult = {
                valid: false,
                errors: [
                    {
                        type: "error",
                        message:
                            e instanceof Error
                                ? e.message
                                : "Validation failed",
                        path: "/",
                    },
                ],
                warnings: [],
            };
        } finally {
            isValidating = false;
        }
    }

    // Computed values
    let errorCount = $derived(validationResult?.errors.length ?? 0);
    let warningCount = $derived(validationResult?.warnings.length ?? 0);
    let filteredErrors = $derived(
        showErrors ? (validationResult?.errors ?? []) : [],
    );
    let filteredWarnings = $derived(
        showWarnings ? (validationResult?.warnings ?? []) : [],
    );
    let allIssues = $derived([...filteredErrors, ...filteredWarnings]);
</script>

<div class="validation-panel" class:collapsed={panelCollapsed}>
    <button
        class="panel-header"
        onclick={() => (panelCollapsed = !panelCollapsed)}
        aria-expanded={!panelCollapsed}
        aria-controls="validation-panel-content"
    >
        <div class="panel-title">
            <span class="panel-icon">🔍</span>
            <span>Schema Validation</span>
            {#if validationResult}
                <span
                    class="status-badge"
                    class:valid={validationResult.valid}
                    class:invalid={!validationResult.valid}
                >
                    {#if validationResult.valid}
                        ✓ Valid
                    {:else}
                        {errorCount} error{errorCount !== 1 ? "s" : ""}, {warningCount}
                        warning{warningCount !== 1 ? "s" : ""}
                    {/if}
                </span>
            {/if}
        </div>
        <span class="collapse-indicator" aria-hidden="true">
            {panelCollapsed ? "▶" : "▼"}
        </span>
    </button>

    {#if !panelCollapsed}
        <div class="panel-content" id="validation-panel-content">
            <!-- Schema Loading Section -->
            <div class="schema-section">
                <div class="section-header">
                    <span class="section-title"
                        >Loaded Schemas ({loadedSchemas.length})</span
                    >
                </div>

                <div class="schema-list">
                    {#each loadedSchemas as schema}
                        <span class="schema-tag loaded">
                            {schema === "_builtin_" ? "Built-in" : schema}
                        </span>
                    {/each}

                    {#if loadingSchemas}
                        <span class="schema-tag loading"
                            >Loading schemas...</span
                        >
                    {/if}
                </div>

                {#if schemaLoadError}
                    <div class="error-message">
                        {schemaLoadError}
                    </div>
                {/if}

                {#if !x4DataPath}
                    <div class="hint">
                        💡 Set your X4 data path on the home page to auto-load
                        official schemas
                    </div>
                {/if}
            </div>

            <!-- Validation Controls -->
            <div class="controls-section">
                <label class="checkbox-label">
                    <input type="checkbox" bind:checked={autoValidate} />
                    Auto-validate
                </label>
                <label class="checkbox-label">
                    <input type="checkbox" bind:checked={showErrors} />
                    Show Errors ({errorCount})
                </label>
                <label class="checkbox-label">
                    <input type="checkbox" bind:checked={showWarnings} />
                    Show Warnings ({warningCount})
                </label>
                <button
                    class="btn-small btn-primary"
                    onclick={validateDocument}
                    disabled={isValidating || !rootNode}
                >
                    {isValidating ? "Validating..." : "Validate Now"}
                </button>
            </div>

            <!-- Validation Results -->
            {#if validationResult}
                <div class="results-section">
                    {#if allIssues.length === 0}
                        <div class="no-issues">
                            {#if validationResult.valid}
                                <span class="success-icon">✅</span>
                                <span
                                    >Document is valid according to loaded
                                    schemas</span
                                >
                            {:else}
                                <span>No issues to display (check filters)</span
                                >
                            {/if}
                        </div>
                    {:else}
                        <div class="issues-list">
                            {#each allIssues as issue}
                                <div
                                    class="issue-item"
                                    class:error={issue.type === "error"}
                                    class:warning={issue.type === "warning"}
                                >
                                    <div class="issue-header">
                                        <span class="issue-icon">
                                            {issue.type === "error"
                                                ? "❌"
                                                : "⚠️"}
                                        </span>
                                        <span class="issue-path"
                                            >{issue.path}</span
                                        >
                                    </div>
                                    <div class="issue-message">
                                        {issue.message}
                                    </div>
                                    {#if issue.suggestion}
                                        <div class="issue-suggestion">
                                            💡 {issue.suggestion}
                                        </div>
                                    {/if}
                                </div>
                            {/each}
                        </div>
                    {/if}
                </div>
            {:else if rootNode}
                <div class="no-results">
                    Click "Validate Now" to check your document
                </div>
            {:else}
                <div class="no-results">Load an XML document to validate</div>
            {/if}
        </div>
    {/if}
</div>

<style>
    .validation-panel {
        background: #1e293b;
        border: 1px solid #334155;
        border-radius: 8px;
        overflow: hidden;
    }

    .validation-panel.collapsed {
        border-radius: 8px;
    }

    .panel-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        width: 100%;
        padding: 12px 16px;
        background: #0f172a;
        cursor: pointer;
        user-select: none;
        border: none;
        font-family: inherit;
        text-align: left;
    }

    .panel-header:hover {
        background: #1e293b;
    }

    .panel-header:focus {
        outline: 2px solid #3b82f6;
        outline-offset: -2px;
    }

    .collapse-indicator {
        color: #94a3b8;
        font-size: 12px;
    }

    .panel-title {
        display: flex;
        align-items: center;
        gap: 8px;
        font-weight: 600;
        color: #e2e8f0;
    }

    .panel-icon {
        font-size: 16px;
    }

    .status-badge {
        padding: 2px 8px;
        border-radius: 12px;
        font-size: 11px;
        font-weight: 500;
    }

    .status-badge.valid {
        background: rgba(34, 197, 94, 0.2);
        color: #4ade80;
    }

    .status-badge.invalid {
        background: rgba(239, 68, 68, 0.2);
        color: #f87171;
    }

    .panel-content {
        padding: 16px;
        display: flex;
        flex-direction: column;
        gap: 16px;
    }

    .schema-section {
        display: flex;
        flex-direction: column;
        gap: 8px;
    }

    .section-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
    }

    .section-title {
        font-size: 12px;
        font-weight: 600;
        color: #94a3b8;
        text-transform: uppercase;
        letter-spacing: 0.05em;
    }

    .schema-list {
        display: flex;
        flex-wrap: wrap;
        gap: 6px;
    }

    .schema-tag {
        padding: 4px 10px;
        border-radius: 4px;
        font-size: 12px;
        font-family: "JetBrains Mono", monospace;
    }

    .schema-tag.loaded {
        background: rgba(59, 130, 246, 0.2);
        color: #60a5fa;
        border: 1px solid rgba(59, 130, 246, 0.3);
    }

    .schema-tag.loading {
        background: rgba(234, 179, 8, 0.2);
        color: #facc15;
        border: 1px solid rgba(234, 179, 8, 0.3);
    }

    .error-message {
        padding: 8px 12px;
        background: rgba(239, 68, 68, 0.1);
        border: 1px solid rgba(239, 68, 68, 0.3);
        border-radius: 4px;
        color: #f87171;
        font-size: 13px;
    }

    .hint {
        font-size: 12px;
        color: #64748b;
    }

    .controls-section {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: 16px;
        padding: 12px;
        background: #0f172a;
        border-radius: 6px;
    }

    .checkbox-label {
        display: flex;
        align-items: center;
        gap: 6px;
        font-size: 13px;
        color: #cbd5e1;
        cursor: pointer;
    }

    .checkbox-label input {
        cursor: pointer;
    }

    .btn-small {
        padding: 6px 12px;
        border-radius: 4px;
        font-size: 12px;
        font-weight: 500;
        border: none;
        cursor: pointer;
        transition: all 0.15s;
        background: #334155;
        color: #e2e8f0;
    }

    .btn-small:hover:not(:disabled) {
        background: #475569;
    }

    .btn-small:disabled {
        opacity: 0.5;
        cursor: not-allowed;
    }

    .btn-small.btn-primary {
        background: #3b82f6;
        color: white;
    }

    .btn-small.btn-primary:hover:not(:disabled) {
        background: #2563eb;
    }

    .results-section {
        max-height: 300px;
        overflow-y: auto;
    }

    .no-issues {
        display: flex;
        align-items: center;
        gap: 8px;
        padding: 16px;
        background: rgba(34, 197, 94, 0.1);
        border: 1px solid rgba(34, 197, 94, 0.2);
        border-radius: 6px;
        color: #4ade80;
        font-size: 14px;
    }

    .success-icon {
        font-size: 18px;
    }

    .no-results {
        padding: 16px;
        text-align: center;
        color: #64748b;
        font-size: 13px;
    }

    .issues-list {
        display: flex;
        flex-direction: column;
        gap: 8px;
    }

    .issue-item {
        padding: 12px;
        border-radius: 6px;
        border: 1px solid;
    }

    .issue-item.error {
        background: rgba(239, 68, 68, 0.05);
        border-color: rgba(239, 68, 68, 0.3);
    }

    .issue-item.warning {
        background: rgba(234, 179, 8, 0.05);
        border-color: rgba(234, 179, 8, 0.3);
    }

    .issue-header {
        display: flex;
        align-items: center;
        gap: 8px;
        margin-bottom: 4px;
    }

    .issue-icon {
        font-size: 14px;
    }

    .issue-path {
        font-family: "JetBrains Mono", monospace;
        font-size: 11px;
        color: #94a3b8;
        background: rgba(0, 0, 0, 0.2);
        padding: 2px 6px;
        border-radius: 3px;
    }

    .issue-message {
        color: #e2e8f0;
        font-size: 13px;
        margin-left: 22px;
    }

    .issue-suggestion {
        margin-top: 6px;
        margin-left: 22px;
        font-size: 12px;
        color: #60a5fa;
    }

    /* Scrollbar styling */
    .results-section::-webkit-scrollbar {
        width: 8px;
    }

    .results-section::-webkit-scrollbar-track {
        background: #1e293b;
    }

    .results-section::-webkit-scrollbar-thumb {
        background: #475569;
        border-radius: 4px;
    }

    .results-section::-webkit-scrollbar-thumb:hover {
        background: #64748b;
    }
</style>
