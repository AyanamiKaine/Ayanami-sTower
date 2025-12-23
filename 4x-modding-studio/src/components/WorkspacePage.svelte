<script lang="ts">
    import type {
        ModInfo,
        ModResponse,
        ModFile,
        ModContents,
    } from "../pages/api/mod";

    // State
    let extensionsPath = $state("");
    let mods = $state<ModInfo[]>([]);
    let selectedMod = $state<ModInfo | null>(null);
    let isLoading = $state(false);
    let error = $state<string | null>(null);
    let successMessage = $state<string | null>(null);

    // Mod contents state
    let modContents = $state<ModContents | null>(null);
    let isLoadingContents = $state(false);
    let expandedCategories = $state<Set<string>>(
        new Set(["libraries", "md", "aiscripts", "t"]),
    );

    // Create mod form
    let showCreateForm = $state(false);
    let newModId = $state("");
    let newModName = $state("");
    let newModAuthor = $state("");
    let newModVersion = $state("1.0.0");
    let newModDescription = $state("");
    let isCreating = $state(false);

    // Load saved paths from localStorage
    $effect(() => {
        if (typeof localStorage !== "undefined") {
            const savedExtPath = localStorage.getItem("x4-extensions-path");
            if (savedExtPath) {
                extensionsPath = savedExtPath;
                loadMods();
            }

            const savedModPath = localStorage.getItem("x4-active-mod");
            if (savedModPath) {
                // Will be set after mods are loaded
            }
        }
    });

    // Load mod contents when selected mod changes
    $effect(() => {
        if (selectedMod) {
            loadModContents(selectedMod.path);
        } else {
            modContents = null;
        }
    });

    // Auto-generate mod ID from name
    $effect(() => {
        if (newModName && !newModId) {
            newModId = newModName
                .toLowerCase()
                .replace(/[^a-z0-9]+/g, "_")
                .replace(/^_|_$/g, "");
        }
    });

    async function loadMods() {
        if (!extensionsPath) return;

        isLoading = true;
        error = null;

        try {
            const response = await fetch("/api/mod", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    action: "list",
                    extensionsPath,
                }),
            });

            const data: ModResponse = await response.json();

            if (data.success && data.mods) {
                mods = data.mods;

                // Restore selected mod
                const savedModPath = localStorage.getItem("x4-active-mod");
                if (savedModPath) {
                    const found = mods.find((m) => m.path === savedModPath);
                    if (found) {
                        selectedMod = found;
                    }
                }
            } else {
                error = data.error || "Failed to load mods";
            }
        } catch (e) {
            error = e instanceof Error ? e.message : "Failed to load mods";
        } finally {
            isLoading = false;
        }
    }

    async function loadModContents(modPath: string) {
        isLoadingContents = true;

        try {
            const response = await fetch("/api/mod", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    action: "get-contents",
                    modPath,
                }),
            });

            const data: ModResponse = await response.json();

            if (data.success && data.contents) {
                modContents = data.contents;
            } else {
                modContents = null;
            }
        } catch (e) {
            modContents = null;
        } finally {
            isLoadingContents = false;
        }
    }

    function saveExtensionsPath() {
        if (extensionsPath && typeof localStorage !== "undefined") {
            // Normalize path
            let normalized = extensionsPath
                .replace(/\/+$/, "")
                .replace(/\/+/g, "/");
            extensionsPath = normalized;
            localStorage.setItem("x4-extensions-path", normalized);
            loadMods();
        }
    }

    function selectMod(mod: ModInfo) {
        // Don't allow selecting official DLC mods
        if (mod.isDLC) {
            error =
                "Cannot select official DLC mods for editing. You can only modify third-party mods.";
            setTimeout(() => (error = null), 4000);
            return;
        }
        selectedMod = mod;
        if (typeof localStorage !== "undefined") {
            localStorage.setItem("x4-active-mod", mod.path);
        }
        successMessage = `Selected: ${mod.name}`;
        setTimeout(() => (successMessage = null), 2000);
    }

    function toggleCategory(category: string) {
        const newSet = new Set(expandedCategories);
        if (newSet.has(category)) {
            newSet.delete(category);
        } else {
            newSet.add(category);
        }
        expandedCategories = newSet;
    }

    function getFilesByCategory(files: ModFile[]): Map<string, ModFile[]> {
        const grouped = new Map<string, ModFile[]>();
        for (const file of files) {
            const category = file.category || "other";
            if (!grouped.has(category)) {
                grouped.set(category, []);
            }
            grouped.get(category)!.push(file);
        }
        return grouped;
    }

    function formatFileSize(bytes: number): string {
        if (bytes < 1024) return `${bytes} B`;
        if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
        return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
    }

    function getCategoryIcon(category: string): string {
        const icons: Record<string, string> = {
            config: "⚙️",
            libraries: "📚",
            md: "🎬",
            aiscripts: "🤖",
            t: "🌍",
            maps: "🗺️",
            assets: "🎨",
            ui: "🖥️",
            music: "🎵",
            sfx: "🔊",
            voice: "🗣️",
            other: "📄",
        };
        return icons[category] || "📁";
    }

    function getFileTypeIcon(type: ModFile["type"]): string {
        switch (type) {
            case "patch":
                return "🔧";
            case "new":
                return "✨";
            case "config":
                return "⚙️";
            default:
                return "📄";
        }
    }

    function getFileTypeLabel(type: ModFile["type"]): string {
        switch (type) {
            case "patch":
                return "Patch";
            case "new":
                return "New";
            case "config":
                return "Config";
            default:
                return "File";
        }
    }

    async function createMod() {
        if (!newModId || !newModName || !extensionsPath) return;

        isCreating = true;
        error = null;

        try {
            const response = await fetch("/api/mod", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    action: "create",
                    extensionsPath,
                    modId: newModId,
                    modName: newModName,
                    author: newModAuthor || "Unknown",
                    version: newModVersion || "1.0.0",
                    description: newModDescription,
                }),
            });

            const data: ModResponse = await response.json();

            if (data.success && data.mod) {
                mods = [...mods, data.mod];
                selectedMod = data.mod;
                localStorage.setItem("x4-active-mod", data.mod.path);

                // Reset form
                showCreateForm = false;
                newModId = "";
                newModName = "";
                newModDescription = "";

                successMessage = `Created mod: ${data.mod.name}`;
                setTimeout(() => (successMessage = null), 3000);
            } else {
                error = data.error || "Failed to create mod";
            }
        } catch (e) {
            error = e instanceof Error ? e.message : "Failed to create mod";
        } finally {
            isCreating = false;
        }
    }

    function openModFolder(mod: ModInfo) {
        // This would ideally open the folder in the file manager
        // For now, we'll copy the path to clipboard
        navigator.clipboard.writeText(mod.path);
        successMessage = "Path copied to clipboard!";
        setTimeout(() => (successMessage = null), 2000);
    }

    function getModTypeIcon(isDLC?: boolean): string {
        return isDLC ? "🎮" : "📦";
    }

    function getModTypeLabel(isDLC?: boolean): string {
        return isDLC ? "Official DLC" : "Third-party Mod";
    }
</script>

<div class="workspace-page">
    <header class="page-header">
        <a href="/" class="back-link">
            <span>←</span> Back to Home
        </a>
        <h1>📦 Mod Workspace</h1>
    </header>

    <div class="workspace-container">
        <!-- Setup Section -->
        <section class="setup-section">
            <div class="setup-card">
                <h2>📁 Extensions Directory</h2>
                <p>
                    Set the path to your X4 extensions folder where mods are
                    installed
                </p>

                <div class="path-input-row">
                    <input
                        type="text"
                        class="path-input"
                        placeholder="/path/to/X4/extensions or ~/.local/share/Steam/steamapps/common/X4 Foundations/extensions"
                        bind:value={extensionsPath}
                        onkeydown={(e) =>
                            e.key === "Enter" && saveExtensionsPath()}
                    />
                    <button
                        class="btn btn-primary"
                        onclick={saveExtensionsPath}
                    >
                        Set Path
                    </button>
                </div>

                <p class="hint">
                    💡 This is typically in your X4 installation folder or in a
                    user documents location
                </p>
            </div>
        </section>

        {#if error}
            <div class="error-banner">
                <span>⚠️</span>
                {error}
                <button class="dismiss-btn" onclick={() => (error = null)}
                    >×</button
                >
            </div>
        {/if}

        {#if successMessage}
            <div class="success-banner">
                <span>✓</span>
                {successMessage}
            </div>
        {/if}

        {#if extensionsPath}
            <!-- Active Mod Section -->
            <section class="active-mod-section">
                <div class="section-header">
                    <h2>🎯 Active Mod</h2>
                    {#if !selectedMod}
                        <span class="hint-text"
                            >Select or create a mod to start working</span
                        >
                    {/if}
                </div>

                {#if selectedMod}
                    <div class="active-mod-card" class:dlc={selectedMod.isDLC}>
                        <div class="mod-icon">
                            {getModTypeIcon(selectedMod.isDLC)}
                        </div>
                        <div class="mod-details">
                            <div class="mod-title-row">
                                <h3>{selectedMod.name}</h3>
                                <span
                                    class="mod-type-badge-large"
                                    class:dlc={selectedMod.isDLC}
                                >
                                    {getModTypeLabel(selectedMod.isDLC)}
                                </span>
                            </div>
                            <p class="mod-meta">
                                <span class="mod-id">{selectedMod.id}</span>
                                <span class="mod-version"
                                    >v{selectedMod.version}</span
                                >
                                <span class="mod-author"
                                    >by {selectedMod.author}</span
                                >
                            </p>
                            {#if selectedMod.description}
                                <p class="mod-description">
                                    {selectedMod.description}
                                </p>
                            {/if}
                            <p class="mod-path" title={selectedMod.path}>
                                📂 {selectedMod.path}
                            </p>
                        </div>
                        <div class="mod-actions">
                            <button
                                class="btn btn-secondary btn-sm"
                                onclick={() => openModFolder(selectedMod!)}
                            >
                                📋 Copy Path
                            </button>
                            <a href="/editor" class="btn btn-primary btn-sm">
                                ✏️ Open Editor
                            </a>
                        </div>
                    </div>

                    <!-- Mod Contents Section -->
                    <div class="mod-contents-section">
                        <div class="contents-header">
                            <h3>📁 Mod Contents</h3>
                            {#if modContents}
                                <div class="contents-stats">
                                    <span class="stat" title="Total files"
                                        >📄 {modContents.stats.totalFiles}</span
                                    >
                                    <span class="stat patch" title="Patch files"
                                        >🔧 {modContents.stats.patchFiles}</span
                                    >
                                    <span class="stat new" title="New files"
                                        >✨ {modContents.stats.newFiles}</span
                                    >
                                </div>
                            {/if}
                            <button
                                class="btn btn-sm btn-secondary"
                                onclick={() =>
                                    selectedMod &&
                                    loadModContents(selectedMod.path)}
                                disabled={isLoadingContents}
                            >
                                🔄 Refresh
                            </button>
                        </div>

                        {#if isLoadingContents}
                            <div class="loading-contents">
                                <div class="spinner small"></div>
                                <span>Loading contents...</span>
                            </div>
                        {:else if modContents && modContents.files.length > 0}
                            <div class="contents-tree">
                                {#each Array.from(getFilesByCategory(modContents.files)) as [category, files] (category)}
                                    <div class="category-group">
                                        <button
                                            class="category-header"
                                            onclick={() =>
                                                toggleCategory(category)}
                                        >
                                            <span class="expand-icon"
                                                >{expandedCategories.has(
                                                    category,
                                                )
                                                    ? "▼"
                                                    : "▶"}</span
                                            >
                                            <span class="category-icon"
                                                >{getCategoryIcon(
                                                    category,
                                                )}</span
                                            >
                                            <span class="category-name"
                                                >{category}</span
                                            >
                                            <span class="category-count"
                                                >{files.length}</span
                                            >
                                        </button>

                                        {#if expandedCategories.has(category)}
                                            <div class="category-files">
                                                {#each files as file (file.relativePath)}
                                                    <div
                                                        class="file-entry"
                                                        class:patch={file.type ===
                                                            "patch"}
                                                        class:new={file.type ===
                                                            "new"}
                                                    >
                                                        <span
                                                            class="file-type-icon"
                                                            title={getFileTypeLabel(
                                                                file.type,
                                                            )}
                                                            >{getFileTypeIcon(
                                                                file.type,
                                                            )}</span
                                                        >
                                                        <span
                                                            class="file-name"
                                                            title={file.relativePath}
                                                            >{file.name}</span
                                                        >
                                                        <span class="file-path"
                                                            >{file.relativePath.replace(
                                                                /\/[^/]+$/,
                                                                "",
                                                            )}</span
                                                        >
                                                        <span class="file-size"
                                                            >{formatFileSize(
                                                                file.size,
                                                            )}</span
                                                        >
                                                    </div>
                                                {/each}
                                            </div>
                                        {/if}
                                    </div>
                                {/each}
                            </div>
                        {:else if modContents}
                            <div class="empty-contents">
                                <p>
                                    📭 This mod is empty. Add some files to get
                                    started!
                                </p>
                            </div>
                        {/if}
                    </div>
                {:else}
                    <div class="no-active-mod">
                        <p>
                            No mod selected. Choose from your mods below or
                            create a new one.
                        </p>
                    </div>
                {/if}
            </section>

            <!-- Mods List Section -->
            <section class="mods-section">
                <div class="section-header">
                    <h2>📚 Your Mods</h2>
                    <div class="section-actions">
                        <button
                            class="btn btn-secondary"
                            onclick={loadMods}
                            disabled={isLoading}
                        >
                            🔄 Refresh
                        </button>
                        <button
                            class="btn btn-primary"
                            onclick={() => (showCreateForm = true)}
                        >
                            ➕ New Mod
                        </button>
                    </div>
                </div>

                {#if isLoading}
                    <div class="loading">
                        <div class="spinner"></div>
                        <span>Loading mods...</span>
                    </div>
                {:else if mods.length === 0}
                    <div class="empty-state">
                        <div class="empty-icon">📦</div>
                        <h3>No Mods Found</h3>
                        <p>Create your first mod to get started!</p>
                        <button
                            class="btn btn-primary"
                            onclick={() => (showCreateForm = true)}
                        >
                            ➕ Create New Mod
                        </button>
                    </div>
                {:else}
                    <div class="mods-grid">
                        {#each mods as mod (mod.path)}
                            <button
                                class="mod-card"
                                class:selected={selectedMod?.path === mod.path}
                                class:dlc={mod.isDLC}
                                class:third-party={!mod.isDLC}
                                class:disabled={mod.isDLC}
                                disabled={mod.isDLC}
                                title={mod.isDLC
                                    ? "Official DLC mods cannot be edited"
                                    : ""}
                                onclick={() => selectMod(mod)}
                            >
                                <div class="mod-card-header">
                                    <span class="mod-card-icon"
                                        >{getModTypeIcon(mod.isDLC)}</span
                                    >
                                    <span class="mod-card-name">{mod.name}</span
                                    >
                                    <span
                                        class="mod-type-badge"
                                        class:dlc={mod.isDLC}
                                    >
                                        {getModTypeLabel(mod.isDLC)}
                                    </span>
                                    {#if selectedMod?.path === mod.path}
                                        <span class="active-badge">Active</span>
                                    {/if}
                                </div>
                                <div class="mod-card-meta">
                                    <span>{mod.id}</span>
                                    <span>v{mod.version}</span>
                                </div>
                                {#if mod.description}
                                    <p class="mod-card-desc">
                                        {mod.description}
                                    </p>
                                {/if}
                            </button>
                        {/each}
                    </div>
                {/if}
            </section>
        {/if}
    </div>

    <!-- Create Mod Modal -->
    {#if showCreateForm}
        <!-- svelte-ignore a11y_no_static_element_interactions -->
        <!-- svelte-ignore a11y_click_events_have_key_events -->
        <div
            class="modal-overlay"
            onclick={() => (showCreateForm = false)}
            role="dialog"
            aria-modal="true"
            tabindex="-1"
        >
            <!-- svelte-ignore a11y_no_static_element_interactions -->
            <!-- svelte-ignore a11y_click_events_have_key_events -->
            <div class="modal-content" onclick={(e) => e.stopPropagation()}>
                <div class="modal-header">
                    <h2>➕ Create New Mod</h2>
                    <button
                        class="close-btn"
                        onclick={() => (showCreateForm = false)}>×</button
                    >
                </div>

                <form
                    class="modal-body"
                    onsubmit={(e) => {
                        e.preventDefault();
                        createMod();
                    }}
                >
                    <div class="form-field">
                        <label for="mod-name">Mod Name *</label>
                        <input
                            id="mod-name"
                            type="text"
                            bind:value={newModName}
                            placeholder="My Awesome Mod"
                            required
                        />
                    </div>

                    <div class="form-field">
                        <label for="mod-id">Mod ID *</label>
                        <input
                            id="mod-id"
                            type="text"
                            bind:value={newModId}
                            placeholder="my_awesome_mod"
                            pattern="[a-z0-9_]+"
                            required
                        />
                        <span class="field-hint"
                            >Lowercase letters, numbers, and underscores only</span
                        >
                    </div>

                    <div class="form-row">
                        <div class="form-field">
                            <label for="mod-author">Author</label>
                            <input
                                id="mod-author"
                                type="text"
                                bind:value={newModAuthor}
                                placeholder="Your Name"
                            />
                        </div>

                        <div class="form-field">
                            <label for="mod-version">Version</label>
                            <input
                                id="mod-version"
                                type="text"
                                bind:value={newModVersion}
                                placeholder="1.0.0"
                            />
                        </div>
                    </div>

                    <div class="form-field">
                        <label for="mod-desc">Description</label>
                        <textarea
                            id="mod-desc"
                            bind:value={newModDescription}
                            placeholder="What does your mod do?"
                            rows="3"
                        ></textarea>
                    </div>

                    <div class="modal-footer">
                        <button
                            type="button"
                            class="btn btn-secondary"
                            onclick={() => (showCreateForm = false)}
                        >
                            Cancel
                        </button>
                        <button
                            type="submit"
                            class="btn btn-primary"
                            disabled={isCreating || !newModId || !newModName}
                        >
                            {isCreating ? "Creating..." : "Create Mod"}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    {/if}
</div>

<style>
    .workspace-page {
        display: flex;
        flex-direction: column;
        min-height: 100vh;
        background: #0f172a;
        color: #e2e8f0;
    }

    .page-header {
        display: flex;
        align-items: center;
        gap: 24px;
        padding: 16px 24px;
        background: #1e293b;
        border-bottom: 1px solid #334155;
    }

    .back-link {
        display: flex;
        align-items: center;
        gap: 8px;
        color: #94a3b8;
        text-decoration: none;
        font-size: 14px;
        transition: color 0.2s;
    }

    .back-link:hover {
        color: #3b82f6;
    }

    .page-header h1 {
        margin: 0;
        font-size: 20px;
    }

    .workspace-container {
        flex: 1;
        padding: 24px;
        max-width: 1200px;
        margin: 0 auto;
        width: 100%;
    }

    /* Setup Section */
    .setup-section {
        margin-bottom: 24px;
    }

    .setup-card {
        background: #1e293b;
        border: 1px solid #334155;
        border-radius: 12px;
        padding: 24px;
    }

    .setup-card h2 {
        margin: 0 0 8px 0;
        font-size: 18px;
    }

    .setup-card > p {
        margin: 0 0 16px 0;
        color: #94a3b8;
        font-size: 14px;
    }

    .path-input-row {
        display: flex;
        gap: 12px;
    }

    .path-input {
        flex: 1;
        padding: 12px 16px;
        background: #0f172a;
        border: 1px solid #334155;
        border-radius: 8px;
        color: #e2e8f0;
        font-size: 14px;
    }

    .path-input:focus {
        outline: none;
        border-color: #3b82f6;
    }

    .hint {
        margin-top: 12px;
        font-size: 13px;
        color: #64748b;
    }

    /* Buttons */
    .btn {
        padding: 12px 20px;
        border-radius: 8px;
        font-size: 14px;
        font-weight: 500;
        cursor: pointer;
        transition: all 0.2s;
        border: none;
        text-decoration: none;
        display: inline-flex;
        align-items: center;
        gap: 6px;
    }

    .btn-primary {
        background: linear-gradient(135deg, #3b82f6, #8b5cf6);
        color: white;
    }

    .btn-primary:hover:not(:disabled) {
        transform: translateY(-1px);
        box-shadow: 0 4px 12px rgba(59, 130, 246, 0.3);
    }

    .btn-secondary {
        background: #334155;
        color: #e2e8f0;
    }

    .btn-secondary:hover:not(:disabled) {
        background: #475569;
    }

    .btn-sm {
        padding: 8px 14px;
        font-size: 13px;
    }

    .btn:disabled {
        opacity: 0.5;
        cursor: not-allowed;
    }

    /* Banners */
    .error-banner,
    .success-banner {
        display: flex;
        align-items: center;
        gap: 8px;
        padding: 12px 16px;
        border-radius: 8px;
        margin-bottom: 16px;
        font-size: 14px;
    }

    .error-banner {
        background: rgba(239, 68, 68, 0.1);
        border: 1px solid rgba(239, 68, 68, 0.3);
        color: #fca5a5;
    }

    .success-banner {
        background: rgba(34, 197, 94, 0.1);
        border: 1px solid rgba(34, 197, 94, 0.3);
        color: #86efac;
    }

    .dismiss-btn {
        margin-left: auto;
        background: none;
        border: none;
        color: inherit;
        font-size: 18px;
        cursor: pointer;
        opacity: 0.7;
    }

    .dismiss-btn:hover {
        opacity: 1;
    }

    /* Sections */
    .section-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        margin-bottom: 16px;
    }

    .section-header h2 {
        margin: 0;
        font-size: 18px;
    }

    .section-actions {
        display: flex;
        gap: 8px;
    }

    .hint-text {
        font-size: 13px;
        color: #64748b;
    }

    /* Active Mod Section */
    .active-mod-section {
        margin-bottom: 32px;
    }

    .active-mod-card {
        display: flex;
        gap: 20px;
        padding: 24px;
        background: linear-gradient(
            135deg,
            rgba(59, 130, 246, 0.1),
            rgba(139, 92, 246, 0.1)
        );
        border: 1px solid rgba(59, 130, 246, 0.3);
        border-radius: 12px;
    }

    .active-mod-card.dlc {
        background: linear-gradient(
            135deg,
            rgba(245, 158, 11, 0.1),
            rgba(249, 115, 22, 0.1)
        );
        border-color: rgba(245, 158, 11, 0.3);
    }

    .mod-icon {
        font-size: 48px;
        flex-shrink: 0;
    }

    .mod-details {
        flex: 1;
    }

    .mod-title-row {
        display: flex;
        align-items: center;
        gap: 12px;
        margin-bottom: 8px;
    }

    .mod-details h3 {
        margin: 0;
        font-size: 20px;
        flex: 1;
    }

    .mod-type-badge-large {
        padding: 4px 12px;
        background: #334155;
        border-radius: 6px;
        font-size: 12px;
        font-weight: 600;
        color: #94a3b8;
        white-space: nowrap;
    }

    .mod-type-badge-large.dlc {
        background: rgba(245, 158, 11, 0.2);
        color: #f59e0b;
    }

    .mod-meta {
        display: flex;
        gap: 12px;
        margin: 0 0 8px 0;
        font-size: 13px;
    }

    .mod-id {
        color: #67e8f9;
        font-family: monospace;
    }

    .mod-version {
        color: #a5b4fc;
    }

    .mod-author {
        color: #94a3b8;
    }

    .mod-description {
        margin: 0 0 8px 0;
        color: #94a3b8;
        font-size: 14px;
    }

    .mod-path {
        margin: 0;
        font-size: 12px;
        color: #64748b;
        font-family: monospace;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
    }

    .mod-actions {
        display: flex;
        flex-direction: column;
        gap: 8px;
    }

    .no-active-mod {
        padding: 24px;
        background: #1e293b;
        border: 1px dashed #334155;
        border-radius: 12px;
        text-align: center;
        color: #64748b;
    }

    /* Mods Grid */
    .mods-section {
        margin-bottom: 32px;
    }

    .mods-grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
        gap: 16px;
    }

    .mod-card {
        display: flex;
        flex-direction: column;
        padding: 16px;
        background: #1e293b;
        border: 1px solid #334155;
        border-radius: 10px;
        cursor: pointer;
        text-align: left;
        transition: all 0.2s;
        color: #e2e8f0;
    }

    .mod-card:hover:not(:disabled) {
        border-color: #475569;
        transform: translateY(-2px);
    }

    .mod-card:disabled {
        cursor: not-allowed;
        opacity: 0.6;
    }

    .mod-card.dlc:disabled:hover {
        border-color: #f59e0b;
        background: rgba(245, 158, 11, 0.05);
        transform: none;
    }

    .mod-card.selected {
        border-color: #3b82f6;
        background: rgba(59, 130, 246, 0.1);
    }

    .mod-card.dlc {
        border-color: #f59e0b;
        background: rgba(245, 158, 11, 0.05);
    }

    .mod-card.dlc:hover:not(:disabled) {
        border-color: #d97706;
        background: rgba(245, 158, 11, 0.1);
    }

    .mod-card.dlc.selected {
        border-color: #f59e0b;
        background: rgba(245, 158, 11, 0.15);
    }

    .mod-card.third-party {
        border-color: #334155;
    }

    .mod-card.third-party:hover:not(:disabled) {
        border-color: #475569;
    }

    .mod-card.third-party.selected {
        border-color: #3b82f6;
        background: rgba(59, 130, 246, 0.1);
    }

    .mod-card-header {
        display: flex;
        align-items: center;
        gap: 10px;
        margin-bottom: 8px;
        flex-wrap: wrap;
    }

    .mod-card-icon {
        font-size: 24px;
    }

    .mod-card-name {
        flex: 1;
        font-weight: 600;
        font-size: 15px;
    }

    .mod-type-badge {
        padding: 2px 8px;
        background: #334155;
        border-radius: 4px;
        font-size: 10px;
        font-weight: 600;
        color: #94a3b8;
        white-space: nowrap;
    }

    .mod-type-badge.dlc {
        background: rgba(245, 158, 11, 0.2);
        color: #f59e0b;
    }

    .active-badge {
        padding: 2px 8px;
        background: #22c55e;
        border-radius: 4px;
        font-size: 11px;
        font-weight: 600;
        color: white;
        white-space: nowrap;
    }

    .mod-card-meta {
        display: flex;
        gap: 10px;
        font-size: 12px;
        color: #64748b;
        margin-bottom: 8px;
    }

    .mod-card-desc {
        margin: 0;
        font-size: 13px;
        color: #94a3b8;
        display: -webkit-box;
        -webkit-line-clamp: 2;
        line-clamp: 2;
        -webkit-box-orient: vertical;
        overflow: hidden;
    }

    /* Empty State */
    .empty-state {
        display: flex;
        flex-direction: column;
        align-items: center;
        padding: 48px;
        text-align: center;
    }

    .empty-icon {
        font-size: 64px;
        opacity: 0.5;
        margin-bottom: 16px;
    }

    .empty-state h3 {
        margin: 0 0 8px 0;
    }

    .empty-state p {
        margin: 0 0 20px 0;
        color: #64748b;
    }

    /* Loading */
    .loading {
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 12px;
        padding: 48px;
        color: #94a3b8;
    }

    .spinner {
        width: 24px;
        height: 24px;
        border: 2px solid #334155;
        border-top-color: #3b82f6;
        border-radius: 50%;
        animation: spin 0.8s linear infinite;
    }

    @keyframes spin {
        to {
            transform: rotate(360deg);
        }
    }

    /* Modal */
    .modal-overlay {
        position: fixed;
        inset: 0;
        background: rgba(0, 0, 0, 0.7);
        backdrop-filter: blur(4px);
        display: flex;
        align-items: center;
        justify-content: center;
        z-index: 1000;
    }

    .modal-content {
        width: 90%;
        max-width: 500px;
        background: #1e293b;
        border: 1px solid #334155;
        border-radius: 16px;
        overflow: hidden;
    }

    .modal-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        padding: 16px 20px;
        border-bottom: 1px solid #334155;
    }

    .modal-header h2 {
        margin: 0;
        font-size: 18px;
    }

    .close-btn {
        background: none;
        border: none;
        color: #94a3b8;
        font-size: 24px;
        cursor: pointer;
    }

    .close-btn:hover {
        color: #e2e8f0;
    }

    .modal-body {
        padding: 20px;
    }

    .form-field {
        margin-bottom: 16px;
    }

    .form-field label {
        display: block;
        margin-bottom: 6px;
        font-size: 13px;
        font-weight: 500;
        color: #94a3b8;
    }

    .form-field input,
    .form-field textarea {
        width: 100%;
        padding: 10px 14px;
        background: #0f172a;
        border: 1px solid #334155;
        border-radius: 6px;
        color: #e2e8f0;
        font-size: 14px;
    }

    .form-field input:focus,
    .form-field textarea:focus {
        outline: none;
        border-color: #3b82f6;
    }

    .form-field textarea {
        resize: vertical;
        min-height: 80px;
    }

    .field-hint {
        display: block;
        margin-top: 4px;
        font-size: 11px;
        color: #64748b;
    }

    .form-row {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 16px;
    }

    .modal-footer {
        display: flex;
        justify-content: flex-end;
        gap: 12px;
        padding-top: 16px;
        border-top: 1px solid #334155;
        margin-top: 8px;
    }

    /* Mod Contents Section */
    .mod-contents-section {
        margin-top: 20px;
        background: #0f172a;
        border: 1px solid #334155;
        border-radius: 12px;
        overflow: hidden;
    }

    .contents-header {
        display: flex;
        align-items: center;
        gap: 16px;
        padding: 12px 16px;
        background: #1e293b;
        border-bottom: 1px solid #334155;
    }

    .contents-header h3 {
        margin: 0;
        font-size: 14px;
        font-weight: 500;
    }

    .contents-stats {
        display: flex;
        gap: 12px;
        margin-left: auto;
    }

    .contents-stats .stat {
        font-size: 12px;
        color: #94a3b8;
        padding: 2px 8px;
        background: #334155;
        border-radius: 4px;
    }

    .contents-stats .stat.patch {
        color: #fbbf24;
        background: rgba(251, 191, 36, 0.1);
    }

    .contents-stats .stat.new {
        color: #34d399;
        background: rgba(52, 211, 153, 0.1);
    }

    .loading-contents {
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 8px;
        padding: 24px;
        color: #94a3b8;
        font-size: 13px;
    }

    .spinner.small {
        width: 16px;
        height: 16px;
    }

    .contents-tree {
        max-height: 400px;
        overflow-y: auto;
    }

    .category-group {
        border-bottom: 1px solid #334155;
    }

    .category-group:last-child {
        border-bottom: none;
    }

    .category-header {
        display: flex;
        align-items: center;
        gap: 8px;
        width: 100%;
        padding: 10px 16px;
        background: #1e293b;
        border: none;
        color: #e2e8f0;
        font-size: 13px;
        cursor: pointer;
        transition: background 0.2s;
        text-align: left;
    }

    .category-header:hover {
        background: #334155;
    }

    .expand-icon {
        font-size: 10px;
        color: #64748b;
        width: 12px;
    }

    .category-icon {
        font-size: 14px;
    }

    .category-name {
        font-weight: 500;
        text-transform: capitalize;
    }

    .category-count {
        margin-left: auto;
        font-size: 11px;
        color: #64748b;
        background: #334155;
        padding: 2px 6px;
        border-radius: 4px;
    }

    .category-files {
        background: #0f172a;
    }

    .file-entry {
        display: flex;
        align-items: center;
        gap: 8px;
        padding: 8px 16px 8px 40px;
        font-size: 12px;
        border-bottom: 1px solid #1e293b;
        transition: background 0.2s;
    }

    .file-entry:last-child {
        border-bottom: none;
    }

    .file-entry:hover {
        background: #1e293b;
    }

    .file-entry.patch {
        border-left: 2px solid #fbbf24;
    }

    .file-entry.new {
        border-left: 2px solid #34d399;
    }

    .file-type-icon {
        font-size: 12px;
    }

    .file-name {
        font-weight: 500;
        color: #e2e8f0;
        white-space: nowrap;
    }

    .file-path {
        color: #64748b;
        font-size: 11px;
        flex: 1;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
    }

    .file-size {
        color: #64748b;
        font-size: 11px;
        white-space: nowrap;
    }

    .empty-contents {
        padding: 32px;
        text-align: center;
        color: #64748b;
    }

    .empty-contents p {
        margin: 0;
        font-size: 13px;
    }
</style>
