<script lang="ts">
    import { untrack } from "svelte";
    import { parseXML, type XMLNode } from "../lib/xml/parser";
    import { generateXPath } from "../lib/xml/xpath-generator";
    import {
        createDiffState,
        addOperation,
        removeOperation,
        generateDiffXML,
        createReplaceOperation,
        createAddOperation,
        createRemoveOperation,
        type DiffState,
        type DiffOperation,
        type AddPosition,
    } from "../lib/xml/diff-generator";
    import type { ModInfo, ModResponse } from "../pages/api/mod";

    import XMLTreeNode from "./XMLTreeNode.svelte";
    import DiffOperationsPanel from "./DiffOperationsPanel.svelte";
    import ContextMenu, { type ContextAction } from "./ContextMenu.svelte";
    import Modal from "./Modal.svelte";
    import SchemaValidationPanel from "./SchemaValidationPanel.svelte";

    // Props
    interface Props {
        initialFilePath?: string | null;
    }

    let { initialFilePath = null }: Props = $props();

    // Sample X4 XML for demonstration
    const SAMPLE_XML = `<?xml version="1.0" encoding="utf-8"?>
<factions>
  <faction id="argon" name="Argon Federation" description="The Argon Federation is a democratic society...">
    <relation faction="paranid" relation="0.5"/>
    <relation faction="teladi" relation="0.3"/>
    <relation faction="xenon" relation="-1"/>
  </faction>
  <faction id="paranid" name="Paranid Empire" description="The Paranid are a deeply religious species...">
    <relation faction="argon" relation="0.5"/>
    <relation faction="teladi" relation="0.1"/>
    <relation faction="xenon" relation="-1"/>
  </faction>
  <faction id="xenon" name="Xenon" description="Hostile AI machines...">
    <relation faction="argon" relation="-1"/>
    <relation faction="paranid" relation="-1"/>
    <relation faction="teladi" relation="-1"/>
  </faction>
</factions>`;

    // State
    let xmlInput = $state(SAMPLE_XML);
    let currentFileName = $state("factions.xml");
    let diffState = $state<DiffState>(createDiffState("factions.xml"));
    let loading = $state(false);

    // Selection state
    let selectedNodeId = $state<string | null>(null);
    let expandedNodes = $state<Set<string>>(new Set());

    // Context menu state
    let contextMenuVisible = $state(false);
    let contextMenuX = $state(0);
    let contextMenuY = $state(0);
    let contextMenuNode = $state<XMLNode | null>(null);

    // Modal state
    let modalVisible = $state(false);
    let modalMode = $state<"replace" | "add">("replace");
    let modalNode = $state<XMLNode | null>(null);
    let modalPosition = $state<AddPosition>("append");
    let modalValue = $state("");
    let modalDescription = $state("");

    // Save to mod state
    let saveModalVisible = $state(false);
    let activeMod = $state<ModInfo | null>(null);
    let isSaving = $state(false);
    let saveError = $state<string | null>(null);
    let saveSuccess = $state<string | null>(null);

    // X4 data path for schema validation
    let x4DataPath = $state<string>("");

    // Load active mod and x4DataPath from localStorage
    $effect(() => {
        if (typeof localStorage !== "undefined") {
            const savedModPath = localStorage.getItem("x4-active-mod");
            if (savedModPath) {
                loadModInfo(savedModPath);
            }
            const savedX4Path = localStorage.getItem("x4-data-path");
            if (savedX4Path) {
                x4DataPath = savedX4Path;
            }
        }
    });

    async function loadModInfo(modPath: string) {
        try {
            const response = await fetch("/api/mod", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    action: "get-info",
                    modPath,
                }),
            });
            const data: ModResponse = await response.json();
            if (data.success && data.mod) {
                activeMod = data.mod;
            }
        } catch (e) {
            console.error("Failed to load mod info:", e);
        }
    }

    async function handleSaveToMod() {
        if (!activeMod || !initialFilePath) {
            saveModalVisible = true;
            return;
        }

        isSaving = true;
        saveError = null;
        saveSuccess = null;

        try {
            const response = await fetch("/api/mod", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    action: "save-diff",
                    modPath: activeMod.path,
                    sourceFile: initialFilePath,
                    diffContent: diffOutput,
                }),
            });

            const data: ModResponse = await response.json();

            if (data.success) {
                saveSuccess = `Saved to: ${data.savedPath}`;
                setTimeout(() => (saveSuccess = null), 5000);
            } else {
                saveError = data.error || "Failed to save";
            }
        } catch (e) {
            saveError = e instanceof Error ? e.message : "Failed to save";
        } finally {
            isSaving = false;
        }
    }

    // Parse XML - use $derived to compute from xmlInput
    let parsedXML = $derived(parseXML(xmlInput));

    // Generate diff output - use $derived
    let diffOutput = $derived(generateDiffXML(diffState));

    // Track the last xmlInput that was used for auto-expand (non-reactive)
    let lastExpandedForInput = "";

    // Load file from path if provided
    async function loadFileFromPath(path: string) {
        loading = true;
        try {
            const response = await fetch(
                `/api/files/read?path=${encodeURIComponent(path)}`,
            );
            const data = await response.json();

            if (data.success && data.content) {
                xmlInput = data.content;
                const fileName = path.split("/").pop() || "file.xml";
                currentFileName = fileName;
                diffState = createDiffState(fileName);
            }
        } catch (e) {
            console.error("Failed to load file:", e);
        } finally {
            loading = false;
        }
    }

    // Load initial file if path provided
    $effect(() => {
        if (initialFilePath) {
            loadFileFromPath(initialFilePath);
        }
    });

    // Auto-expand root and first level when XML changes
    $effect(() => {
        const root = parsedXML?.root;
        const currentInput = xmlInput;

        // Use untrack to read lastExpandedForInput without creating a dependency
        const shouldExpand = untrack(
            () => currentInput !== lastExpandedForInput,
        );

        if (root && shouldExpand) {
            // Auto-expand root and first level
            const toExpand = new Set<string>();
            toExpand.add(root.id);
            for (const child of root.children) {
                if (child.type === "element") {
                    toExpand.add(child.id);
                }
            }
            expandedNodes = toExpand;
            lastExpandedForInput = currentInput;
        }
    });

    function handleNodeSelect(node: XMLNode) {
        selectedNodeId = node.id;
    }

    function handleToggleExpand(nodeId: string) {
        const newExpanded = new Set(expandedNodes);
        if (newExpanded.has(nodeId)) {
            newExpanded.delete(nodeId);
        } else {
            newExpanded.add(nodeId);
        }
        expandedNodes = newExpanded;
    }

    function handleContextMenu(node: XMLNode, event: MouseEvent) {
        contextMenuNode = node;
        contextMenuX = event.clientX;
        contextMenuY = event.clientY;
        contextMenuVisible = true;
    }

    function handleContextAction(action: ContextAction) {
        const xpath = generateXPath(action.node);

        if (action.type === "remove") {
            // Direct removal - no modal needed
            diffState = addOperation(
                diffState,
                createRemoveOperation(
                    xpath.selector,
                    `Remove <${action.node.name}> element`,
                    xpath.confidence,
                    xpath.warning,
                ),
            );
        } else if (action.type === "replace") {
            // Open modal for replace
            modalMode = "replace";
            modalNode = action.node;
            modalValue = getNodeTextContent(action.node);
            modalDescription = `Modify <${action.node.name}>`;
            modalVisible = true;
        } else if (action.type === "add") {
            // Open modal for add
            modalMode = "add";
            modalNode = action.node;
            modalPosition = action.position || "append";
            modalValue = `<newElement attribute="value" />`;
            modalDescription = `Add new element`;
            modalVisible = true;
        }
    }

    function getNodeTextContent(node: XMLNode): string {
        const textChild = node.children.find((c) => c.type === "text");
        return textChild?.textContent || "";
    }

    function handleModalConfirm() {
        if (!modalNode) return;

        const xpath = generateXPath(modalNode);

        if (modalMode === "replace") {
            diffState = addOperation(
                diffState,
                createReplaceOperation(
                    xpath.selector,
                    modalValue,
                    modalDescription,
                    xpath.confidence,
                    xpath.warning,
                ),
            );
        } else {
            diffState = addOperation(
                diffState,
                createAddOperation(
                    xpath.selector,
                    modalValue,
                    modalPosition,
                    modalDescription,
                    xpath.confidence,
                    xpath.warning,
                ),
            );
        }

        modalVisible = false;
    }

    function handleRemoveOperation(id: string) {
        diffState = removeOperation(diffState, id);
    }

    function handleEditOperation(operation: DiffOperation) {
        // For now, just log - could open a modal to edit
        console.log("Edit operation:", operation);
    }

    function handleCopyDiff() {
        navigator.clipboard.writeText(diffOutput);
    }

    function handleExpandAll() {
        if (!parsedXML?.nodeMap) return;
        expandedNodes = new Set(parsedXML.nodeMap.keys());
    }

    function handleCollapseAll() {
        if (!parsedXML?.root) return;
        expandedNodes = new Set([parsedXML.root.id]);
    }

    function handleLoadFile(event: Event) {
        const input = event.target as HTMLInputElement;
        const file = input.files?.[0];
        if (!file) return;

        const reader = new FileReader();
        reader.onload = (e) => {
            xmlInput = e.target?.result as string;
            currentFileName = file.name;
            diffState = createDiffState(file.name);
        };
        reader.readAsText(file);
    }
</script>

<div class="xpath-generator">
    <!-- Header -->
    <header class="header">
        <div class="header-left">
            <a href="/" class="back-link" title="Back to Home"> ← Home </a>
            <h1 class="title">
                <span class="title-icon">🎯</span>
                XPath Generator
            </h1>
            {#if currentFileName}
                <span class="current-file">📄 {currentFileName}</span>
            {/if}
        </div>
        <div class="header-center">
            {#if activeMod}
                <div class="active-mod-badge" title={activeMod.path}>
                    <span class="mod-icon">📦</span>
                    <span class="mod-name">{activeMod.name}</span>
                    <a href="/workspace" class="change-mod">Change</a>
                </div>
            {:else}
                <a href="/workspace" class="btn btn-outline btn-sm">
                    📦 Select Mod
                </a>
            {/if}
        </div>
        <div class="header-right">
            {#if loading}
                <span class="loading-indicator">Loading...</span>
            {/if}
            <label class="file-input-label">
                <input
                    type="file"
                    accept=".xml"
                    onchange={handleLoadFile}
                    class="file-input"
                />
                <span class="btn btn-secondary">📂 Load XML</span>
            </label>
            <a href="/browser" class="btn btn-secondary">🗂️ Browse</a>
        </div>
    </header>

    <!-- Save notification bar -->
    {#if saveSuccess}
        <div class="save-success-bar">
            <span>✓</span>
            {saveSuccess}
        </div>
    {/if}
    {#if saveError}
        <div class="save-error-bar">
            <span>⚠️</span>
            {saveError}
            <button class="dismiss-btn" onclick={() => (saveError = null)}
                >×</button
            >
        </div>
    {/if}

    <!-- Main Content -->
    <div class="main-content">
        <!-- Left Panel: XML Tree -->
        <div class="panel tree-panel">
            <div class="panel-header">
                <h2 class="panel-title">XML Structure</h2>
                <div class="panel-actions">
                    <button
                        class="btn-icon"
                        onclick={handleExpandAll}
                        title="Expand All"
                    >
                        ⊞
                    </button>
                    <button
                        class="btn-icon"
                        onclick={handleCollapseAll}
                        title="Collapse All"
                    >
                        ⊟
                    </button>
                </div>
            </div>
            <div class="panel-content tree-content">
                {#if parsedXML?.errors?.length}
                    <div class="error-banner">
                        <span class="error-icon">⚠️</span>
                        {parsedXML.errors[0]}
                    </div>
                {/if}

                {#if parsedXML?.root}
                    <XMLTreeNode
                        node={parsedXML.root}
                        {selectedNodeId}
                        {expandedNodes}
                        onSelect={handleNodeSelect}
                        onContextMenu={handleContextMenu}
                        onToggleExpand={handleToggleExpand}
                    />
                {/if}
            </div>
        </div>

        <!-- Middle Panel: Operations -->
        <div class="panel operations-panel-container">
            <DiffOperationsPanel
                operations={diffState.operations}
                onRemove={handleRemoveOperation}
                onEdit={handleEditOperation}
            />
        </div>

        <!-- Right Panel: Diff Output -->
        <div class="panel output-panel">
            <div class="panel-header">
                <h2 class="panel-title">diff.xml Output</h2>
                <div class="panel-header-actions">
                    <button
                        class="btn btn-secondary btn-sm"
                        onclick={handleCopyDiff}
                    >
                        📋 Copy
                    </button>
                    {#if activeMod && initialFilePath}
                        <button
                            class="btn btn-primary btn-sm"
                            onclick={handleSaveToMod}
                            disabled={isSaving ||
                                diffState.operations.length === 0}
                        >
                            {isSaving ? "💾 Saving..." : "💾 Save to Mod"}
                        </button>
                    {:else if !activeMod}
                        <a
                            href="/workspace"
                            class="btn btn-outline btn-sm"
                            title="Select a mod first"
                        >
                            📦 Select Mod to Save
                        </a>
                    {:else}
                        <button
                            class="btn btn-secondary btn-sm"
                            disabled
                            title="Load a file from X4 data to enable saving"
                        >
                            💾 Save
                        </button>
                    {/if}
                </div>
            </div>
            <div class="panel-content">
                <pre class="diff-output"><code>{diffOutput}</code></pre>
            </div>
        </div>
    </div>

    <!-- Schema Validation Panel -->
    <div class="validation-panel-container">
        <SchemaValidationPanel
            rootNode={parsedXML?.root ?? null}
            {x4DataPath}
        />
    </div>

    <!-- Context Menu -->
    <ContextMenu
        visible={contextMenuVisible}
        x={contextMenuX}
        y={contextMenuY}
        node={contextMenuNode}
        onAction={handleContextAction}
        onClose={() => (contextMenuVisible = false)}
    />

    <!-- Edit Modal -->
    <Modal
        visible={modalVisible}
        title={modalMode === "replace" ? "Modify Value" : "Add Element"}
        onClose={() => (modalVisible = false)}
        onConfirm={handleModalConfirm}
        confirmText={modalMode === "replace" ? "Apply Change" : "Add Element"}
    >
        {#snippet children()}
            <div class="modal-form">
                {#if modalNode}
                    <div class="form-field">
                        <span class="field-label">Target XPath</span>
                        <code class="xpath-display"
                            >{generateXPath(modalNode).selector}</code
                        >
                    </div>
                {/if}

                <div class="form-field">
                    <label class="field-label" for="modal-description"
                        >Description (optional)</label
                    >
                    <input
                        id="modal-description"
                        type="text"
                        class="field-input"
                        bind:value={modalDescription}
                        placeholder="What does this change do?"
                    />
                </div>

                {#if modalMode === "add"}
                    <div class="form-field">
                        <label class="field-label" for="modal-position"
                            >Position</label
                        >
                        <select
                            id="modal-position"
                            class="field-select"
                            bind:value={modalPosition}
                        >
                            <option value="before">Before this node</option>
                            <option value="after">After this node</option>
                            <option value="prepend"
                                >Prepend inside (first child)</option
                            >
                            <option value="append"
                                >Append inside (last child)</option
                            >
                        </select>
                    </div>
                {/if}

                <div class="form-field">
                    <label class="field-label" for="modal-value">
                        {modalMode === "replace" ? "New Value" : "XML Content"}
                    </label>
                    <textarea
                        id="modal-value"
                        class="field-textarea"
                        bind:value={modalValue}
                        rows="6"
                        placeholder={modalMode === "replace"
                            ? "Enter new value..."
                            : '<element attr="value" />'}
                    ></textarea>
                </div>
            </div>
        {/snippet}
    </Modal>
</div>

<style>
    .xpath-generator {
        display: flex;
        flex-direction: column;
        height: 100vh;
        background: #0f172a;
        color: #e2e8f0;
    }

    .header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        padding: 12px 24px;
        background: #1e293b;
        border-bottom: 1px solid #334155;
        gap: 16px;
    }

    .header-left {
        display: flex;
        align-items: center;
        gap: 16px;
    }

    .header-right {
        display: flex;
        align-items: center;
        gap: 12px;
    }

    .header-center {
        display: flex;
        align-items: center;
        justify-content: center;
        flex: 1;
    }

    .active-mod-badge {
        display: flex;
        align-items: center;
        gap: 8px;
        padding: 6px 12px;
        background: rgba(34, 197, 94, 0.15);
        border: 1px solid rgba(34, 197, 94, 0.3);
        border-radius: 6px;
        font-size: 13px;
    }

    .mod-icon {
        font-size: 16px;
    }

    .mod-name {
        color: #4ade80;
        font-weight: 500;
    }

    .change-mod {
        color: #94a3b8;
        text-decoration: none;
        font-size: 12px;
        margin-left: 8px;
        padding: 2px 6px;
        border-radius: 4px;
        transition: all 0.15s;
    }

    .change-mod:hover {
        color: #e2e8f0;
        background: rgba(255, 255, 255, 0.1);
    }

    .back-link {
        color: #94a3b8;
        text-decoration: none;
        font-size: 14px;
        padding: 6px 12px;
        border-radius: 6px;
        transition: all 0.15s;
    }

    .back-link:hover {
        color: #e2e8f0;
        background: rgba(255, 255, 255, 0.1);
    }

    .title {
        display: flex;
        align-items: center;
        gap: 10px;
        margin: 0;
        font-size: 18px;
        font-weight: 700;
        color: #f8fafc;
    }

    .title-icon {
        font-size: 20px;
    }

    .current-file {
        font-size: 14px;
        color: #67e8f9;
        padding: 4px 12px;
        background: rgba(103, 232, 249, 0.1);
        border-radius: 4px;
    }

    .loading-indicator {
        font-size: 14px;
        color: #94a3b8;
    }

    .file-input {
        display: none;
    }

    .file-input-label {
        cursor: pointer;
    }

    .btn {
        display: inline-flex;
        align-items: center;
        gap: 6px;
        padding: 8px 16px;
        border-radius: 6px;
        font-size: 14px;
        font-weight: 500;
        text-decoration: none;
        cursor: pointer;
        transition: all 0.15s;
        border: none;
    }

    .btn-secondary {
        background: #334155;
        color: #e2e8f0;
    }

    .btn-secondary:hover {
        background: #475569;
    }

    .btn-primary {
        background: #3b82f6;
        color: white;
    }

    .btn-primary:hover {
        background: #2563eb;
    }

    .btn-sm {
        padding: 6px 12px;
        font-size: 13px;
    }

    .btn-outline {
        background: transparent;
        border: 1px solid #475569;
        color: #94a3b8;
    }

    .btn-outline:hover {
        background: rgba(255, 255, 255, 0.05);
        border-color: #64748b;
        color: #e2e8f0;
    }

    .btn:disabled {
        opacity: 0.5;
        cursor: not-allowed;
    }

    .btn-icon {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 28px;
        height: 28px;
        padding: 0;
        background: none;
        border: none;
        border-radius: 4px;
        cursor: pointer;
        color: #64748b;
        font-size: 16px;
        transition: all 0.15s;
    }

    .btn-icon:hover {
        background: rgba(255, 255, 255, 0.1);
        color: #e2e8f0;
    }

    .main-content {
        flex: 1;
        display: grid;
        grid-template-columns: 1fr 320px 1fr;
        gap: 1px;
        background: #334155;
        overflow: hidden;
        min-height: 0; /* Allow shrinking */
    }

    .validation-panel-container {
        flex-shrink: 0;
        padding: 12px 24px;
        background: #0f172a;
        border-top: 1px solid #334155;
        max-height: 350px;
        overflow-y: auto;
    }

    .panel {
        display: flex;
        flex-direction: column;
        background: #0f172a;
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
        margin: 0;
        font-size: 14px;
        font-weight: 600;
        color: #e2e8f0;
    }

    .panel-actions {
        display: flex;
        gap: 4px;
    }

    .panel-content {
        flex: 1;
        overflow: auto;
    }

    .tree-content {
        padding: 12px 0;
    }

    .error-banner {
        display: flex;
        align-items: center;
        gap: 8px;
        margin: 8px 16px;
        padding: 12px;
        background: rgba(239, 68, 68, 0.1);
        border: 1px solid rgba(239, 68, 68, 0.3);
        border-radius: 6px;
        color: #fca5a5;
        font-size: 13px;
    }

    .error-icon {
        font-size: 16px;
    }

    .diff-output {
        margin: 0;
        padding: 16px;
        font-family: "JetBrains Mono", "Fira Code", monospace;
        font-size: 12px;
        line-height: 1.6;
        white-space: pre-wrap;
        color: #94a3b8;
    }

    .diff-output code {
        color: inherit;
    }

    /* Modal Form Styles */
    .modal-form {
        display: flex;
        flex-direction: column;
        gap: 16px;
    }

    .form-field {
        display: flex;
        flex-direction: column;
        gap: 6px;
    }

    .field-label {
        font-size: 13px;
        font-weight: 500;
        color: #94a3b8;
    }

    .xpath-display {
        padding: 8px 12px;
        background: #0f172a;
        border-radius: 6px;
        font-family: "JetBrains Mono", "Fira Code", monospace;
        font-size: 12px;
        color: #67e8f9;
        word-break: break-all;
    }

    .field-input,
    .field-select,
    .field-textarea {
        padding: 10px 12px;
        background: #0f172a;
        border: 1px solid #334155;
        border-radius: 6px;
        font-size: 14px;
        color: #e2e8f0;
        transition: border-color 0.15s;
    }

    .field-input:focus,
    .field-select:focus,
    .field-textarea:focus {
        outline: none;
        border-color: #3b82f6;
    }

    .field-textarea {
        font-family: "JetBrains Mono", "Fira Code", monospace;
        font-size: 13px;
        resize: vertical;
        min-height: 120px;
    }

    .field-select {
        cursor: pointer;
    }

    /* Panel header actions */
    .panel-header-actions {
        display: flex;
        align-items: center;
        gap: 8px;
    }

    /* Save notification bars */
    .save-success-bar {
        display: flex;
        align-items: center;
        justify-content: space-between;
        padding: 10px 16px;
        background: rgba(34, 197, 94, 0.15);
        border: 1px solid rgba(34, 197, 94, 0.3);
        border-radius: 6px;
        margin-bottom: 12px;
        color: #4ade80;
        font-size: 13px;
    }

    .save-error-bar {
        display: flex;
        align-items: center;
        justify-content: space-between;
        padding: 10px 16px;
        background: rgba(239, 68, 68, 0.15);
        border: 1px solid rgba(239, 68, 68, 0.3);
        border-radius: 6px;
        margin-bottom: 12px;
        color: #f87171;
        font-size: 13px;
    }

    .dismiss-btn {
        background: transparent;
        border: none;
        color: inherit;
        cursor: pointer;
        padding: 2px 6px;
        font-size: 16px;
        opacity: 0.7;
        transition: opacity 0.15s;
    }

    .dismiss-btn:hover {
        opacity: 1;
    }
</style>
