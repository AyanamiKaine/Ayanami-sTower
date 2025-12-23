<script lang="ts">
    import type { XMLNode } from "../lib/xml/parser";
    import type {
        DiffOperationType,
        AddPosition,
    } from "../lib/xml/diff-generator";

    interface Props {
        visible: boolean;
        x: number;
        y: number;
        node: XMLNode | null;
        onAction?: (action: ContextAction) => void;
        onClose?: () => void;
    }

    export interface ContextAction {
        type: DiffOperationType;
        node: XMLNode;
        position?: AddPosition;
    }

    let { visible, x, y, node, onAction, onClose }: Props = $props();

    let menuRef: HTMLDivElement | undefined = $state(undefined);
    let showAddSubmenu = $state(false);

    function handleAction(type: DiffOperationType, position?: AddPosition) {
        if (node) {
            onAction?.({ type, node, position });
        }
        onClose?.();
    }

    function handleClickOutside(e: MouseEvent) {
        if (menuRef && !menuRef.contains(e.target as Node)) {
            onClose?.();
        }
    }

    function handleKeyDown(e: KeyboardEvent) {
        if (e.key === "Escape") {
            onClose?.();
        }
    }

    $effect(() => {
        if (visible) {
            document.addEventListener("click", handleClickOutside);
            document.addEventListener("keydown", handleKeyDown);
            showAddSubmenu = false;
        }
        return () => {
            document.removeEventListener("click", handleClickOutside);
            document.removeEventListener("keydown", handleKeyDown);
        };
    });

    // Adjust position to keep menu in viewport (use defaults for SSR safety)
    let adjustedX = $derived(
        typeof window !== "undefined"
            ? Math.min(x, window.innerWidth - 220)
            : x,
    );
    let adjustedY = $derived(
        typeof window !== "undefined"
            ? Math.min(y, window.innerHeight - 300)
            : y,
    );
</script>

{#if visible && node}
    <div
        bind:this={menuRef}
        class="context-menu"
        style="left: {adjustedX}px; top: {adjustedY}px;"
        role="menu"
    >
        <div class="menu-header">
            <span class="node-name">&lt;{node.name}&gt;</span>
        </div>

        <div class="menu-divider"></div>

        <button
            class="menu-item"
            onclick={() => handleAction("replace")}
            tabindex="0"
        >
            <span class="item-icon">✏️</span>
            <span class="item-label">Modify Value</span>
            <span class="item-hint">Replace content</span>
        </button>

        <div
            class="menu-item has-submenu"
            role="button"
            tabindex="0"
            onmouseenter={() => (showAddSubmenu = true)}
            onmouseleave={() => (showAddSubmenu = false)}
            onfocus={() => (showAddSubmenu = true)}
            onblur={() => (showAddSubmenu = false)}
            onkeydown={(e) =>
                e.key === "Enter" && (showAddSubmenu = !showAddSubmenu)}
        >
            <span class="item-icon">➕</span>
            <span class="item-label">Add Node</span>
            <span class="item-arrow">▶</span>

            {#if showAddSubmenu}
                <div class="submenu" role="menu">
                    <button
                        class="menu-item"
                        onclick={() => handleAction("add", "before")}
                        tabindex="0"
                    >
                        <span class="item-label">Before this node</span>
                    </button>
                    <button
                        class="menu-item"
                        onclick={() => handleAction("add", "after")}
                        tabindex="0"
                    >
                        <span class="item-label">After this node</span>
                    </button>
                    <div class="menu-divider"></div>
                    <button
                        class="menu-item"
                        onclick={() => handleAction("add", "prepend")}
                        tabindex="0"
                    >
                        <span class="item-label">Prepend inside</span>
                    </button>
                    <button
                        class="menu-item"
                        onclick={() => handleAction("add", "append")}
                        tabindex="0"
                    >
                        <span class="item-label">Append inside</span>
                    </button>
                </div>
            {/if}
        </div>

        <button
            class="menu-item danger"
            onclick={() => handleAction("remove")}
            tabindex="0"
        >
            <span class="item-icon">🗑️</span>
            <span class="item-label">Remove Node</span>
            <span class="item-hint">Delete element</span>
        </button>

        <div class="menu-divider"></div>

        <button class="menu-item" onclick={() => onClose?.()} tabindex="0">
            <span class="item-icon">📋</span>
            <span class="item-label">Copy XPath</span>
        </button>
    </div>
{/if}

<style>
    .context-menu {
        position: fixed;
        z-index: 1000;
        min-width: 200px;
        background: #1e293b;
        border: 1px solid #334155;
        border-radius: 8px;
        box-shadow: 0 10px 40px rgba(0, 0, 0, 0.5);
        padding: 4px 0;
        font-size: 13px;
    }

    .menu-header {
        padding: 8px 12px;
        background: #0f172a;
        border-radius: 6px 6px 0 0;
    }

    .node-name {
        font-family: "JetBrains Mono", "Fira Code", monospace;
        font-size: 12px;
        color: #e879f9;
        font-weight: 500;
    }

    .menu-divider {
        height: 1px;
        background: #334155;
        margin: 4px 0;
    }

    .menu-item {
        display: flex;
        align-items: center;
        gap: 10px;
        width: 100%;
        padding: 8px 12px;
        background: none;
        border: none;
        color: #e2e8f0;
        cursor: pointer;
        text-align: left;
        transition: background-color 0.1s;
        position: relative;
    }

    .menu-item:hover {
        background: rgba(59, 130, 246, 0.2);
    }

    .menu-item.danger:hover {
        background: rgba(239, 68, 68, 0.2);
        color: #fca5a5;
    }

    .item-icon {
        width: 18px;
        text-align: center;
        flex-shrink: 0;
    }

    .item-label {
        flex: 1;
    }

    .item-hint {
        font-size: 11px;
        color: #64748b;
    }

    .item-arrow {
        font-size: 10px;
        color: #64748b;
    }

    .has-submenu {
        position: relative;
    }

    .submenu {
        position: absolute;
        left: 100%;
        top: -4px;
        min-width: 160px;
        background: #1e293b;
        border: 1px solid #334155;
        border-radius: 8px;
        box-shadow: 0 10px 40px rgba(0, 0, 0, 0.5);
        padding: 4px 0;
    }
</style>
