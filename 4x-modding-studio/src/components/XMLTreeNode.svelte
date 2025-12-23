<script lang="ts">
    import type { XMLNode } from "../lib/xml/parser";
    import XMLTreeNode from "./XMLTreeNode.svelte";

    interface Props {
        node: XMLNode;
        depth?: number;
        selectedNodeId?: string | null;
        expandedNodes?: Set<string>;
        onSelect?: (node: XMLNode) => void;
        onContextMenu?: (node: XMLNode, event: MouseEvent) => void;
        onToggleExpand?: (nodeId: string) => void;
    }

    let {
        node,
        depth = 0,
        selectedNodeId = null,
        expandedNodes = new Set<string>(),
        onSelect,
        onContextMenu,
        onToggleExpand,
    }: Props = $props();

    let isExpanded = $derived(expandedNodes.has(node.id));
    let isSelected = $derived(selectedNodeId === node.id);
    let isComment = $derived(node.type === "comment");
    let isCdata = $derived(node.type === "cdata");
    let isSpecialNode = $derived(isComment || isCdata);
    // Include elements, comments, and CDATA as visible children
    let visibleChildren = $derived(
        node.children.filter(
            (c) =>
                c.type === "element" ||
                c.type === "comment" ||
                c.type === "cdata",
        ),
    );
    let hasChildren = $derived(visibleChildren.length > 0);
    let elementChildren = $derived(
        node.children.filter((c) => c.type === "element"),
    );
    let textContent = $derived(
        node.children.find((c) => c.type === "text")?.textContent || "",
    );

    function handleClick(e: MouseEvent) {
        e.stopPropagation();
        onSelect?.(node);
    }

    function handleContextMenu(e: MouseEvent) {
        e.preventDefault();
        e.stopPropagation();
        onSelect?.(node);
        onContextMenu?.(node, e);
    }

    function handleToggle(e: MouseEvent) {
        e.stopPropagation();
        onToggleExpand?.(node.id);
    }

    function handleKeyDown(e: KeyboardEvent) {
        if (e.key === "Enter" || e.key === " ") {
            e.preventDefault();
            onSelect?.(node);
        }
    }

    // Get attribute badges - highlight important ones
    const priorityAttrs = ["id", "name", "macro", "class", "type"];
</script>

<div class="xml-node" style="--depth: {depth}">
    <div
        class="node-row"
        class:selected={isSelected}
        class:has-children={hasChildren}
        class:comment-node={isComment}
        class:cdata-node={isCdata}
        onclick={handleClick}
        oncontextmenu={handleContextMenu}
        onkeydown={handleKeyDown}
        role="treeitem"
        tabindex="0"
        aria-selected={isSelected}
        aria-expanded={hasChildren ? isExpanded : undefined}
    >
        <!-- Expand/Collapse Toggle -->
        {#if hasChildren && !isSpecialNode}
            <button
                class="toggle-btn"
                onclick={handleToggle}
                aria-label={isExpanded ? "Collapse" : "Expand"}
            >
                <svg
                    class="toggle-icon"
                    class:expanded={isExpanded}
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    stroke-width="2"
                >
                    <polyline points="9 18 15 12 9 6"></polyline>
                </svg>
            </button>
        {:else if !isSpecialNode}
            <span class="toggle-placeholder"></span>
        {/if}

        <!-- Comment Node -->
        {#if isComment}
            <span class="comment-syntax">&lt;!--</span>
            <span class="comment-content">{node.textContent}</span>
            <span class="comment-syntax">--&gt;</span>
            <!-- CDATA Node -->
        {:else if isCdata}
            <span class="cdata-syntax">&lt;![CDATA[</span>
            <span class="cdata-content">{node.textContent}</span>
            <span class="cdata-syntax">]]&gt;</span>
            <!-- Element Node -->
        {:else}
            <!-- Element Name -->
            <span class="element-name">&lt;{node.name}</span>

            <!-- Attributes -->
            {#each Object.entries(node.attributes) as [key, value]}
                <span
                    class="attribute"
                    class:priority={priorityAttrs.includes(key)}
                >
                    <span class="attr-name">{key}</span>=<span
                        class="attr-value">"{value}"</span
                    >
                </span>
            {/each}

            <!-- Closing bracket -->
            {#if !hasChildren && !textContent}
                <span class="element-name">/&gt;</span>
            {:else}
                <span class="element-name">&gt;</span>
            {/if}

            <!-- Inline text content (if short) -->
            {#if textContent && textContent.length < 50 && !hasChildren}
                <span class="text-content">{textContent}</span>
                <span class="element-name">&lt;/{node.name}&gt;</span>
            {/if}
        {/if}
    </div>

    <!-- Children -->
    {#if hasChildren && isExpanded}
        <div class="children" role="group">
            {#each visibleChildren as child (child.id)}
                <XMLTreeNode
                    node={child}
                    depth={depth + 1}
                    {selectedNodeId}
                    {expandedNodes}
                    {onSelect}
                    {onContextMenu}
                    {onToggleExpand}
                />
            {/each}
        </div>
    {/if}

    <!-- Closing tag for expanded elements with children -->
    {#if hasChildren && isExpanded && !isSpecialNode}
        <div class="closing-tag" style="--depth: {depth}">
            <span class="element-name">&lt;/{node.name}&gt;</span>
        </div>
    {/if}
</div>

<style>
    .xml-node {
        font-family: "JetBrains Mono", "Fira Code", monospace;
        font-size: 13px;
        line-height: 1.6;
    }

    .node-row {
        display: flex;
        align-items: center;
        gap: 2px;
        padding: 2px 8px;
        padding-left: calc(var(--depth) * 20px + 8px);
        cursor: pointer;
        border-radius: 4px;
        transition: background-color 0.15s;
        flex-wrap: wrap;
    }

    .node-row:hover {
        background-color: rgba(59, 130, 246, 0.1);
    }

    .node-row.selected {
        background-color: rgba(59, 130, 246, 0.2);
        outline: 1px solid rgba(59, 130, 246, 0.5);
    }

    .toggle-btn {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 18px;
        height: 18px;
        padding: 0;
        background: none;
        border: none;
        cursor: pointer;
        color: #64748b;
        flex-shrink: 0;
    }

    .toggle-btn:hover {
        color: #3b82f6;
    }

    .toggle-icon {
        width: 14px;
        height: 14px;
        transition: transform 0.15s;
    }

    .toggle-icon.expanded {
        transform: rotate(90deg);
    }

    .toggle-placeholder {
        width: 18px;
        flex-shrink: 0;
    }

    .element-name {
        color: #e879f9;
        font-weight: 500;
    }

    .attribute {
        margin-left: 6px;
    }

    .attribute.priority .attr-name {
        color: #fbbf24;
        font-weight: 600;
    }

    .attr-name {
        color: #67e8f9;
    }

    .attr-value {
        color: #86efac;
    }

    .text-content {
        color: #d1d5db;
        margin: 0 4px;
    }

    .closing-tag {
        padding-left: calc(var(--depth) * 20px + 26px);
        padding-top: 2px;
        padding-bottom: 2px;
    }

    /* Comment styling */
    .comment-node {
        opacity: 0.8;
    }

    .comment-syntax {
        color: #6b7280;
        font-weight: 500;
    }

    .comment-content {
        color: #9ca3af;
        font-style: italic;
        margin: 0 4px;
        white-space: pre-wrap;
        word-break: break-word;
    }

    /* CDATA styling */
    .cdata-node {
        opacity: 0.9;
    }

    .cdata-syntax {
        color: #f59e0b;
        font-weight: 500;
    }

    .cdata-content {
        color: #fbbf24;
        font-family: inherit;
        margin: 0 4px;
        white-space: pre-wrap;
        word-break: break-word;
    }
</style>
