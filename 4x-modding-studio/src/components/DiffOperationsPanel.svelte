<script lang="ts">
    import type { DiffOperation } from "../lib/xml/diff-generator";

    interface Props {
        operations: DiffOperation[];
        onRemove?: (id: string) => void;
        onEdit?: (operation: DiffOperation) => void;
    }

    let { operations, onRemove, onEdit }: Props = $props();

    function getOperationIcon(type: DiffOperation["type"]): string {
        switch (type) {
            case "replace":
                return "✏️";
            case "add":
                return "➕";
            case "remove":
                return "🗑️";
        }
    }

    function getOperationColor(type: DiffOperation["type"]): string {
        switch (type) {
            case "replace":
                return "bg-amber-500/20 border-amber-500/50 text-amber-200";
            case "add":
                return "bg-green-500/20 border-green-500/50 text-green-200";
            case "remove":
                return "bg-red-500/20 border-red-500/50 text-red-200";
        }
    }

    function getConfidenceBadge(confidence: DiffOperation["confidence"]): {
        text: string;
        class: string;
    } {
        switch (confidence) {
            case "high":
                return { text: "High", class: "bg-green-600 text-green-100" };
            case "medium":
                return { text: "Med", class: "bg-yellow-600 text-yellow-100" };
            case "low":
                return { text: "Low", class: "bg-red-600 text-red-100" };
        }
    }

    function truncateSelector(
        selector: string,
        maxLength: number = 60,
    ): string {
        if (selector.length <= maxLength) return selector;
        return selector.slice(0, maxLength - 3) + "...";
    }
</script>

<div class="operations-panel">
    <div class="panel-header">
        <h3 class="panel-title">
            <span class="title-icon">📋</span>
            Diff Operations
        </h3>
        <span class="operation-count">{operations.length}</span>
    </div>

    {#if operations.length === 0}
        <div class="empty-state">
            <div class="empty-icon">🎯</div>
            <p class="empty-text">No operations yet</p>
            <p class="empty-hint">
                Right-click on a node in the XML tree to add operations
            </p>
        </div>
    {:else}
        <div class="operations-list">
            {#each operations as operation, index (operation.id)}
                <div class="operation-card {getOperationColor(operation.type)}">
                    <div class="operation-header">
                        <span class="operation-number">#{index + 1}</span>
                        <span class="operation-icon"
                            >{getOperationIcon(operation.type)}</span
                        >
                        <span class="operation-type"
                            >{operation.type.toUpperCase()}</span
                        >
                        <span
                            class="confidence-badge {getConfidenceBadge(
                                operation.confidence,
                            ).class}"
                        >
                            {getConfidenceBadge(operation.confidence).text}
                        </span>
                        <div class="operation-actions">
                            <button
                                class="action-btn edit-btn"
                                onclick={() => onEdit?.(operation)}
                                title="Edit operation"
                            >
                                ✏️
                            </button>
                            <button
                                class="action-btn remove-btn"
                                onclick={() => onRemove?.(operation.id)}
                                title="Remove operation"
                            >
                                ✕
                            </button>
                        </div>
                    </div>

                    <div class="operation-selector">
                        <code title={operation.selector}
                            >{truncateSelector(operation.selector)}</code
                        >
                    </div>

                    {#if operation.description}
                        <div class="operation-description">
                            {operation.description}
                        </div>
                    {/if}

                    {#if operation.warning}
                        <div class="operation-warning">
                            ⚠️ {operation.warning}
                        </div>
                    {/if}

                    {#if operation.type === "replace" && operation.newValue}
                        <div class="operation-value">
                            <span class="value-label">New value:</span>
                            <code class="value-content"
                                >{operation.newValue}</code
                            >
                        </div>
                    {/if}

                    {#if operation.type === "add" && operation.position}
                        <div class="operation-position">
                            <span class="position-label">Position:</span>
                            <span class="position-value"
                                >{operation.position}</span
                            >
                        </div>
                    {/if}
                </div>
            {/each}
        </div>
    {/if}
</div>

<style>
    .operations-panel {
        display: flex;
        flex-direction: column;
        height: 100%;
        background: #0f172a;
        border-radius: 8px;
        overflow: hidden;
    }

    .panel-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        padding: 12px 16px;
        background: #1e293b;
        border-bottom: 1px solid #334155;
    }

    .panel-title {
        display: flex;
        align-items: center;
        gap: 8px;
        margin: 0;
        font-size: 14px;
        font-weight: 600;
        color: #e2e8f0;
    }

    .title-icon {
        font-size: 16px;
    }

    .operation-count {
        display: flex;
        align-items: center;
        justify-content: center;
        min-width: 24px;
        height: 24px;
        padding: 0 8px;
        background: #3b82f6;
        border-radius: 12px;
        font-size: 12px;
        font-weight: 600;
        color: white;
    }

    .empty-state {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        padding: 40px 20px;
        text-align: center;
    }

    .empty-icon {
        font-size: 48px;
        margin-bottom: 16px;
        opacity: 0.5;
    }

    .empty-text {
        margin: 0;
        font-size: 16px;
        color: #94a3b8;
    }

    .empty-hint {
        margin: 8px 0 0;
        font-size: 13px;
        color: #64748b;
    }

    .operations-list {
        flex: 1;
        overflow-y: auto;
        padding: 12px;
        display: flex;
        flex-direction: column;
        gap: 8px;
    }

    .operation-card {
        padding: 12px;
        border-radius: 6px;
        border: 1px solid;
    }

    .operation-header {
        display: flex;
        align-items: center;
        gap: 8px;
        margin-bottom: 8px;
    }

    .operation-number {
        font-size: 11px;
        font-weight: 600;
        color: #64748b;
    }

    .operation-icon {
        font-size: 14px;
    }

    .operation-type {
        font-size: 11px;
        font-weight: 700;
        letter-spacing: 0.05em;
    }

    .confidence-badge {
        margin-left: auto;
        padding: 2px 6px;
        border-radius: 4px;
        font-size: 10px;
        font-weight: 600;
    }

    .operation-actions {
        display: flex;
        gap: 4px;
    }

    .action-btn {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 24px;
        height: 24px;
        padding: 0;
        background: rgba(255, 255, 255, 0.1);
        border: none;
        border-radius: 4px;
        cursor: pointer;
        font-size: 12px;
        transition: background-color 0.15s;
    }

    .action-btn:hover {
        background: rgba(255, 255, 255, 0.2);
    }

    .remove-btn:hover {
        background: rgba(239, 68, 68, 0.3);
    }

    .operation-selector {
        margin-bottom: 8px;
    }

    .operation-selector code {
        display: block;
        padding: 6px 8px;
        background: rgba(0, 0, 0, 0.3);
        border-radius: 4px;
        font-family: "JetBrains Mono", "Fira Code", monospace;
        font-size: 11px;
        color: #67e8f9;
        word-break: break-all;
    }

    .operation-description {
        font-size: 12px;
        color: #94a3b8;
        margin-bottom: 8px;
    }

    .operation-warning {
        padding: 6px 8px;
        background: rgba(251, 191, 36, 0.1);
        border-radius: 4px;
        font-size: 11px;
        color: #fbbf24;
        margin-bottom: 8px;
    }

    .operation-value,
    .operation-position {
        display: flex;
        align-items: center;
        gap: 8px;
        font-size: 12px;
    }

    .value-label,
    .position-label {
        color: #64748b;
    }

    .value-content {
        padding: 2px 6px;
        background: rgba(0, 0, 0, 0.3);
        border-radius: 3px;
        font-family: "JetBrains Mono", "Fira Code", monospace;
        font-size: 11px;
        color: #86efac;
    }

    .position-value {
        color: #e2e8f0;
        font-weight: 500;
    }
</style>
