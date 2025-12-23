<script lang="ts">
    import type { FileEntry, ListResponse } from "../pages/api/files/list";

    interface Props {
        initialPath?: string;
        onFileSelect?: (path: string, content: string) => void;
    }

    let { initialPath = "", onFileSelect }: Props = $props();

    /**
     * Normalize a file path - remove trailing slashes, handle duplicates
     */
    function normalizePath(path: string): string {
        if (!path) return "";
        // Remove trailing slashes (but keep root /)
        let normalized = path.replace(/\/+$/, "") || "/";
        // Collapse multiple slashes into one
        normalized = normalized.replace(/\/+/g, "/");
        return normalized;
    }

    // State
    let currentPath = $state("");
    let pathInput = $state("");

    // Initialize from props
    $effect(() => {
        const normalized = normalizePath(initialPath);
        if (normalized && !currentPath) {
            currentPath = normalized;
            pathInput = normalized;
            loadDirectory(normalized);
        }
    });
    let entries = $state<FileEntry[]>([]);
    let loading = $state(false);
    let error = $state<string | null>(null);
    let expandedDirs = $state<Set<string>>(new Set());
    let selectedFile = $state<string | null>(null);
    let fileContent = $state<string | null>(null);
    let loadingFile = $state(false);

    // Breadcrumb parts
    let breadcrumbs = $derived(() => {
        if (!currentPath) return [];
        const parts = currentPath.split("/").filter(Boolean);
        let accumulated = "";
        return parts.map((part) => {
            accumulated += "/" + part;
            return { name: part, path: accumulated };
        });
    });

    async function loadDirectory(path: string) {
        if (!path) return;

        // Normalize the path before making request
        const normalizedPath = normalizePath(path);

        loading = true;
        error = null;

        try {
            const response = await fetch(
                `/api/files/list?path=${encodeURIComponent(normalizedPath)}`,
            );
            const data: ListResponse = await response.json();

            if (data.success) {
                currentPath = normalizedPath;
                pathInput = normalizedPath;
                entries = data.entries;
            } else {
                error = data.error || "Failed to load directory";
            }
        } catch (e) {
            error = e instanceof Error ? e.message : "Failed to load directory";
        } finally {
            loading = false;
        }
    }

    async function loadFile(path: string) {
        loadingFile = true;
        selectedFile = path;
        fileContent = null;

        try {
            const response = await fetch(
                `/api/files/read?path=${encodeURIComponent(path)}`,
            );
            const data = await response.json();

            if (data.success) {
                fileContent = data.content;
                onFileSelect?.(path, data.content);
            } else {
                error = data.error || "Failed to read file";
            }
        } catch (e) {
            error = e instanceof Error ? e.message : "Failed to read file";
        } finally {
            loadingFile = false;
        }
    }

    function handlePathSubmit(e: Event) {
        e.preventDefault();
        loadDirectory(pathInput);
    }

    function handleEntryClick(entry: FileEntry) {
        if (entry.type === "directory") {
            loadDirectory(entry.path);
        } else {
            loadFile(entry.path);
        }
    }

    function navigateUp() {
        if (!currentPath) return;
        const parent = currentPath.split("/").slice(0, -1).join("/") || "/";
        loadDirectory(parent);
    }

    function navigateTo(path: string) {
        loadDirectory(path);
    }

    function formatSize(bytes?: number): string {
        if (bytes === undefined) return "";
        if (bytes < 1024) return `${bytes} B`;
        if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
        return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
    }

    function getFileIcon(entry: FileEntry): string {
        if (entry.type === "directory") return "📁";
        switch (entry.extension) {
            case ".xml":
                return "📄";
            case ".xsd":
                return "📋";
            case ".lua":
                return "🌙";
            case ".txt":
                return "📝";
            case ".md":
                return "📖";
            case ".dds":
                return "🖼️";
            default:
                return "📄";
        }
    }

    // Load initial path if provided
    $effect(() => {
        if (initialPath && !currentPath) {
            loadDirectory(initialPath);
        }
    });
</script>

<div class="file-browser">
    <!-- Path Input -->
    <form class="path-form" onsubmit={handlePathSubmit}>
        <div class="path-input-container">
            <span class="path-icon">📂</span>
            <input
                type="text"
                class="path-input"
                placeholder="Enter X4 unpacked data path (e.g., /home/user/X4/unpacked)"
                bind:value={pathInput}
            />
            <button type="submit" class="btn btn-primary" disabled={loading}>
                {loading ? "..." : "Browse"}
            </button>
        </div>
    </form>

    {#if error}
        <div class="error-banner">
            <span class="error-icon">⚠️</span>
            <span>{error}</span>
            <button class="btn-close" onclick={() => (error = null)}>×</button>
        </div>
    {/if}

    {#if currentPath}
        <!-- Breadcrumbs -->
        <nav class="breadcrumbs">
            <button class="breadcrumb-item" onclick={() => navigateTo("/")}>
                🏠
            </button>
            {#each breadcrumbs() as crumb, i}
                <span class="breadcrumb-separator">/</span>
                <button
                    class="breadcrumb-item"
                    class:active={i === breadcrumbs().length - 1}
                    onclick={() => navigateTo(crumb.path)}
                >
                    {crumb.name}
                </button>
            {/each}
        </nav>

        <!-- File List -->
        <div class="file-list">
            {#if currentPath !== "/"}
                <button class="file-entry directory" onclick={navigateUp}>
                    <span class="file-icon">📁</span>
                    <span class="file-name">..</span>
                    <span class="file-meta">Parent Directory</span>
                </button>
            {/if}

            {#each entries as entry}
                <button
                    class="file-entry"
                    class:directory={entry.type === "directory"}
                    class:selected={selectedFile === entry.path}
                    onclick={() => handleEntryClick(entry)}
                >
                    <span class="file-icon">{getFileIcon(entry)}</span>
                    <span class="file-name">{entry.name}</span>
                    <span class="file-meta">
                        {#if entry.type === "file"}
                            {formatSize(entry.size)}
                        {/if}
                    </span>
                </button>
            {/each}

            {#if entries.length === 0 && !loading}
                <div class="empty-state">
                    <span class="empty-icon">📭</span>
                    <p>No files found in this directory</p>
                </div>
            {/if}
        </div>
    {:else}
        <!-- Initial State -->
        <div class="initial-state">
            <div class="initial-icon">🎮</div>
            <h3>Browse X4 Game Data</h3>
            <p>Enter the path to your unpacked X4 data directory above.</p>
            <div class="hint-box">
                <h4>💡 Tip: Extracting X4 Data</h4>
                <p>
                    Use the X4 Foundations Catalog Tool to extract game files:
                </p>
                <code>XRCatTool.exe -in "01.cat" -out "./unpacked"</code>
            </div>
        </div>
    {/if}
</div>

<style>
    .file-browser {
        display: flex;
        flex-direction: column;
        height: 100%;
        background: #0f172a;
    }

    .path-form {
        padding: 16px;
        background: #1e293b;
        border-bottom: 1px solid #334155;
    }

    .path-input-container {
        display: flex;
        align-items: center;
        gap: 8px;
        background: #0f172a;
        border: 1px solid #334155;
        border-radius: 8px;
        padding: 4px 8px;
    }

    .path-icon {
        font-size: 18px;
    }

    .path-input {
        flex: 1;
        background: transparent;
        border: none;
        color: #e2e8f0;
        font-size: 14px;
        padding: 8px;
        outline: none;
    }

    .path-input::placeholder {
        color: #64748b;
    }

    .btn {
        padding: 8px 16px;
        border-radius: 6px;
        font-size: 14px;
        font-weight: 500;
        cursor: pointer;
        transition: all 0.15s;
        border: none;
    }

    .btn-primary {
        background: #3b82f6;
        color: white;
    }

    .btn-primary:hover:not(:disabled) {
        background: #2563eb;
    }

    .btn-primary:disabled {
        opacity: 0.5;
        cursor: not-allowed;
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

    .btn-close {
        margin-left: auto;
        background: none;
        border: none;
        color: #fca5a5;
        cursor: pointer;
        font-size: 18px;
        padding: 0 4px;
    }

    .breadcrumbs {
        display: flex;
        align-items: center;
        gap: 4px;
        padding: 8px 16px;
        background: #1e293b;
        border-bottom: 1px solid #334155;
        overflow-x: auto;
        white-space: nowrap;
    }

    .breadcrumb-item {
        background: none;
        border: none;
        color: #94a3b8;
        cursor: pointer;
        padding: 4px 8px;
        border-radius: 4px;
        font-size: 13px;
        transition: all 0.15s;
    }

    .breadcrumb-item:hover {
        background: rgba(255, 255, 255, 0.1);
        color: #e2e8f0;
    }

    .breadcrumb-item.active {
        color: #3b82f6;
        font-weight: 500;
    }

    .breadcrumb-separator {
        color: #475569;
    }

    .file-list {
        flex: 1;
        overflow-y: auto;
        padding: 8px 0;
    }

    .file-entry {
        display: flex;
        align-items: center;
        gap: 12px;
        width: 100%;
        padding: 10px 16px;
        background: none;
        border: none;
        color: #e2e8f0;
        cursor: pointer;
        text-align: left;
        transition: background 0.15s;
    }

    .file-entry:hover {
        background: rgba(255, 255, 255, 0.05);
    }

    .file-entry.selected {
        background: rgba(59, 130, 246, 0.2);
    }

    .file-entry.directory {
        color: #67e8f9;
    }

    .file-icon {
        font-size: 18px;
        width: 24px;
        text-align: center;
    }

    .file-name {
        flex: 1;
        font-size: 14px;
        overflow: hidden;
        text-overflow: ellipsis;
    }

    .file-meta {
        font-size: 12px;
        color: #64748b;
    }

    .empty-state {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        padding: 48px;
        color: #64748b;
        text-align: center;
    }

    .empty-icon {
        font-size: 48px;
        margin-bottom: 16px;
    }

    .initial-state {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        padding: 48px 24px;
        text-align: center;
    }

    .initial-icon {
        font-size: 64px;
        margin-bottom: 16px;
    }

    .initial-state h3 {
        margin: 0 0 8px 0;
        font-size: 20px;
        color: #e2e8f0;
    }

    .initial-state p {
        margin: 0;
        color: #94a3b8;
    }

    .hint-box {
        margin-top: 24px;
        padding: 16px;
        background: rgba(59, 130, 246, 0.1);
        border: 1px solid rgba(59, 130, 246, 0.3);
        border-radius: 8px;
        text-align: left;
        max-width: 400px;
    }

    .hint-box h4 {
        margin: 0 0 8px 0;
        font-size: 14px;
        color: #67e8f9;
    }

    .hint-box p {
        margin: 0 0 8px 0;
        font-size: 13px;
    }

    .hint-box code {
        display: block;
        padding: 8px;
        background: #0f172a;
        border-radius: 4px;
        font-size: 12px;
        color: #a5b4fc;
        overflow-x: auto;
    }
</style>
