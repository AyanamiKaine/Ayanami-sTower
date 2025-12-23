<script lang="ts">
    import type {
        SearchResponse,
        SearchResult,
        SearchMatch,
    } from "../pages/api/search";

    // State
    let searchQuery = $state("");
    let searchResults = $state<SearchResult[]>([]);
    let isSearching = $state(false);
    let searchError = $state<string | null>(null);
    let totalMatches = $state(0);
    let searchedFiles = $state(0);
    let hasSearched = $state(false);

    // Search options
    let useRegex = $state(false);
    let caseSensitive = $state(false);
    let searchPath = $state("");

    // Expanded results
    let expandedResults = $state<Set<string>>(new Set());

    // Load saved path from localStorage
    $effect(() => {
        if (typeof localStorage !== "undefined") {
            const saved = localStorage.getItem("x4-data-path");
            if (saved) {
                searchPath = saved;
            }
        }
    });

    // Common X4 search suggestions
    const searchSuggestions = [
        { query: "macro name=", description: "Find macro definitions" },
        { query: "component class=", description: "Find component classes" },
        { query: "<faction", description: "Faction definitions" },
        { query: "<ship ", description: "Ship definitions" },
        { query: "<station", description: "Station definitions" },
        { query: "<ware ", description: "Ware/trade goods" },
        { query: "<job ", description: "Job/spawn definitions" },
        { query: "<mission", description: "Mission definitions" },
        { query: 'race="', description: "Race references" },
        { query: "<connection", description: "Connection points" },
        { query: "<aiscript", description: "AI scripts" },
        { query: "<cue ", description: "Mission director cues" },
    ];

    async function performSearch() {
        if (!searchQuery.trim() || !searchPath) return;

        isSearching = true;
        searchError = null;
        hasSearched = true;
        expandedResults = new Set();

        try {
            const params = new URLSearchParams({
                q: searchQuery,
                path: searchPath,
                regex: useRegex.toString(),
                case: caseSensitive.toString(),
                max: "100",
            });

            const response = await fetch(`/api/search?${params}`);
            const data: SearchResponse = await response.json();

            if (data.success) {
                searchResults = data.results;
                totalMatches = data.totalMatches;
                searchedFiles = data.searchedFiles;
            } else {
                searchError = data.error || "Search failed";
                searchResults = [];
            }
        } catch (e) {
            searchError = e instanceof Error ? e.message : "Search failed";
            searchResults = [];
        } finally {
            isSearching = false;
        }
    }

    function handleKeyDown(e: KeyboardEvent) {
        if (e.key === "Enter") {
            performSearch();
        }
    }

    function applySuggestion(query: string) {
        searchQuery = query;
        performSearch();
    }

    function toggleExpanded(filePath: string) {
        const newExpanded = new Set(expandedResults);
        if (newExpanded.has(filePath)) {
            newExpanded.delete(filePath);
        } else {
            newExpanded.add(filePath);
        }
        expandedResults = newExpanded;
    }

    function openInEditor(filePath: string) {
        window.location.href = `/editor?file=${encodeURIComponent(filePath)}`;
    }

    function highlightMatch(content: string, query: string): string {
        if (useRegex) {
            try {
                const regex = new RegExp(
                    `(${query})`,
                    caseSensitive ? "g" : "gi",
                );
                return content.replace(regex, "<mark>$1</mark>");
            } catch {
                return escapeHtml(content);
            }
        }
        const escaped = escapeHtml(content);
        const escapedQuery = escapeHtml(query);
        const regex = new RegExp(
            `(${escapeRegexStr(escapedQuery)})`,
            caseSensitive ? "g" : "gi",
        );
        return escaped.replace(regex, "<mark>$1</mark>");
    }

    function escapeHtml(str: string): string {
        return str
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;");
    }

    function escapeRegexStr(str: string): string {
        return str.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
    }

    function getFileIcon(path: string): string {
        if (path.includes("/md/") || path.includes("/aiscripts/")) return "📜";
        if (path.includes("/libraries/")) return "📚";
        if (path.includes("/maps/")) return "🗺️";
        if (path.includes("/t/")) return "🌐";
        return "📄";
    }
</script>

<div class="database-page">
    <header class="page-header">
        <a href="/" class="back-link">
            <span>←</span> Back to Home
        </a>
        <h1>🔍 X4 Database Search</h1>
    </header>

    <div class="search-container">
        <div class="search-box">
            <div class="search-input-row">
                <input
                    type="text"
                    class="search-input"
                    placeholder="Search XML content... (e.g., macro name=, <faction, ship class=)"
                    bind:value={searchQuery}
                    onkeydown={handleKeyDown}
                />
                <button
                    class="btn btn-primary"
                    onclick={performSearch}
                    disabled={isSearching || !searchQuery.trim() || !searchPath}
                >
                    {isSearching ? "🔄 Searching..." : "🔍 Search"}
                </button>
            </div>

            <div class="search-options">
                <label class="option-checkbox">
                    <input type="checkbox" bind:checked={useRegex} />
                    <span>Regex</span>
                </label>
                <label class="option-checkbox">
                    <input type="checkbox" bind:checked={caseSensitive} />
                    <span>Case Sensitive</span>
                </label>
                {#if searchPath}
                    <span class="search-path" title={searchPath}>
                        📁 {searchPath.split("/").pop()}
                    </span>
                {:else}
                    <a href="/" class="no-path-warning"
                        >⚠️ Set X4 data path first</a
                    >
                {/if}
            </div>
        </div>

        <!-- Quick Search Suggestions -->
        {#if !hasSearched}
            <div class="suggestions-section">
                <h3>Quick Searches</h3>
                <div class="suggestions-grid">
                    {#each searchSuggestions as suggestion}
                        <button
                            class="suggestion-btn"
                            onclick={() => applySuggestion(suggestion.query)}
                        >
                            <code>{suggestion.query}</code>
                            <span>{suggestion.description}</span>
                        </button>
                    {/each}
                </div>
            </div>
        {/if}

        <!-- Search Results -->
        {#if hasSearched}
            <div class="results-section">
                {#if searchError}
                    <div class="error-message">
                        <span>⚠️</span>
                        {searchError}
                    </div>
                {:else if isSearching}
                    <div class="loading-message">
                        <div class="spinner"></div>
                        <span>Searching {searchedFiles} XML files...</span>
                    </div>
                {:else if searchResults.length === 0}
                    <div class="no-results">
                        <span class="no-results-icon">🔍</span>
                        <p>
                            No matches found for "<strong>{searchQuery}</strong
                            >"
                        </p>
                        <p class="no-results-hint">
                            Searched {searchedFiles} XML files
                        </p>
                    </div>
                {:else}
                    <div class="results-header">
                        <span class="results-count">
                            Found <strong>{totalMatches}</strong> matches in
                            <strong>{searchResults.length}</strong> files
                        </span>
                        <span class="files-searched"
                            >({searchedFiles} files searched)</span
                        >
                    </div>

                    <div class="results-list">
                        {#each searchResults as result (result.filePath)}
                            <div class="result-item">
                                <div class="result-header">
                                    <button
                                        class="expand-btn"
                                        onclick={() =>
                                            toggleExpanded(result.filePath)}
                                    >
                                        <span class="expand-icon">
                                            {expandedResults.has(
                                                result.filePath,
                                            )
                                                ? "▼"
                                                : "▶"}
                                        </span>
                                        <span class="file-icon"
                                            >{getFileIcon(
                                                result.relativePath,
                                            )}</span
                                        >
                                        <span class="file-path"
                                            >{result.relativePath}</span
                                        >
                                        <span class="match-count"
                                            >{result.matches.length} matches</span
                                        >
                                    </button>
                                    <button
                                        class="open-btn"
                                        onclick={() =>
                                            openInEditor(result.filePath)}
                                        title="Open in Editor"
                                    >
                                        📝 Open
                                    </button>
                                </div>

                                {#if expandedResults.has(result.filePath)}
                                    <div class="result-matches">
                                        {#each result.matches as match, idx}
                                            <div class="match-item">
                                                <span class="line-number"
                                                    >Line {match.line}</span
                                                >
                                                <pre
                                                    class="match-content">{@html highlightMatch(
                                                        match.content,
                                                        searchQuery,
                                                    )}</pre>
                                            </div>
                                        {/each}
                                    </div>
                                {/if}
                            </div>
                        {/each}
                    </div>
                {/if}
            </div>
        {/if}
    </div>
</div>

<style>
    .database-page {
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
        color: #e2e8f0;
    }

    .search-container {
        flex: 1;
        padding: 24px;
        max-width: 1200px;
        margin: 0 auto;
        width: 100%;
    }

    .search-box {
        background: #1e293b;
        border: 1px solid #334155;
        border-radius: 12px;
        padding: 20px;
    }

    .search-input-row {
        display: flex;
        gap: 12px;
    }

    .search-input {
        flex: 1;
        padding: 14px 18px;
        background: #0f172a;
        border: 1px solid #334155;
        border-radius: 8px;
        color: #e2e8f0;
        font-size: 15px;
    }

    .search-input:focus {
        outline: none;
        border-color: #3b82f6;
    }

    .search-input::placeholder {
        color: #64748b;
    }

    .btn {
        padding: 14px 24px;
        border-radius: 8px;
        font-size: 14px;
        font-weight: 500;
        cursor: pointer;
        transition: all 0.2s;
        border: none;
        white-space: nowrap;
    }

    .btn-primary {
        background: linear-gradient(135deg, #3b82f6, #8b5cf6);
        color: white;
    }

    .btn-primary:hover:not(:disabled) {
        transform: translateY(-1px);
        box-shadow: 0 4px 12px rgba(59, 130, 246, 0.3);
    }

    .btn-primary:disabled {
        opacity: 0.5;
        cursor: not-allowed;
    }

    .search-options {
        display: flex;
        align-items: center;
        gap: 20px;
        margin-top: 12px;
        padding-top: 12px;
        border-top: 1px solid #334155;
    }

    .option-checkbox {
        display: flex;
        align-items: center;
        gap: 6px;
        font-size: 13px;
        color: #94a3b8;
        cursor: pointer;
    }

    .option-checkbox input {
        accent-color: #3b82f6;
    }

    .search-path {
        margin-left: auto;
        padding: 4px 10px;
        background: rgba(59, 130, 246, 0.1);
        border-radius: 4px;
        font-size: 12px;
        color: #67e8f9;
    }

    .no-path-warning {
        margin-left: auto;
        color: #fbbf24;
        font-size: 13px;
        text-decoration: none;
    }

    .no-path-warning:hover {
        text-decoration: underline;
    }

    /* Suggestions */
    .suggestions-section {
        margin-top: 32px;
    }

    .suggestions-section h3 {
        margin: 0 0 16px 0;
        font-size: 16px;
        color: #94a3b8;
    }

    .suggestions-grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
        gap: 12px;
    }

    .suggestion-btn {
        display: flex;
        flex-direction: column;
        gap: 4px;
        padding: 12px 16px;
        background: #1e293b;
        border: 1px solid #334155;
        border-radius: 8px;
        cursor: pointer;
        text-align: left;
        transition: all 0.2s;
    }

    .suggestion-btn:hover {
        border-color: #3b82f6;
        background: rgba(59, 130, 246, 0.1);
    }

    .suggestion-btn code {
        font-family: "JetBrains Mono", "Fira Code", monospace;
        font-size: 13px;
        color: #67e8f9;
    }

    .suggestion-btn span {
        font-size: 12px;
        color: #64748b;
    }

    /* Results */
    .results-section {
        margin-top: 24px;
    }

    .error-message {
        display: flex;
        align-items: center;
        gap: 8px;
        padding: 16px;
        background: rgba(239, 68, 68, 0.1);
        border: 1px solid rgba(239, 68, 68, 0.3);
        border-radius: 8px;
        color: #fca5a5;
    }

    .loading-message {
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

    .no-results {
        display: flex;
        flex-direction: column;
        align-items: center;
        padding: 48px;
        color: #94a3b8;
    }

    .no-results-icon {
        font-size: 48px;
        opacity: 0.5;
        margin-bottom: 16px;
    }

    .no-results p {
        margin: 0;
    }

    .no-results-hint {
        font-size: 13px;
        margin-top: 8px !important;
        opacity: 0.7;
    }

    .results-header {
        display: flex;
        align-items: center;
        gap: 12px;
        margin-bottom: 16px;
        font-size: 14px;
        color: #94a3b8;
    }

    .results-count strong {
        color: #22c55e;
    }

    .files-searched {
        opacity: 0.7;
    }

    .results-list {
        display: flex;
        flex-direction: column;
        gap: 8px;
    }

    .result-item {
        background: #1e293b;
        border: 1px solid #334155;
        border-radius: 8px;
        overflow: hidden;
    }

    .result-header {
        display: flex;
        align-items: center;
        gap: 8px;
        padding: 8px 12px 8px 8px;
    }

    .expand-btn {
        display: flex;
        align-items: center;
        gap: 12px;
        flex: 1;
        padding: 8px;
        background: none;
        border: none;
        cursor: pointer;
        text-align: left;
        color: #e2e8f0;
        border-radius: 6px;
        transition: background 0.2s;
    }

    .expand-btn:hover {
        background: rgba(255, 255, 255, 0.05);
    }

    .expand-icon {
        font-size: 10px;
        color: #64748b;
        width: 12px;
    }

    .file-icon {
        font-size: 16px;
    }

    .file-path {
        flex: 1;
        font-family: "JetBrains Mono", "Fira Code", monospace;
        font-size: 13px;
        color: #67e8f9;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
    }

    .match-count {
        padding: 2px 8px;
        background: rgba(59, 130, 246, 0.2);
        border-radius: 4px;
        font-size: 12px;
        color: #93c5fd;
    }

    .open-btn {
        padding: 6px 12px;
        background: #334155;
        border: none;
        border-radius: 4px;
        font-size: 12px;
        color: #e2e8f0;
        cursor: pointer;
        transition: background 0.2s;
    }

    .open-btn:hover {
        background: #475569;
    }

    .result-matches {
        border-top: 1px solid #334155;
        background: #0f172a;
    }

    .match-item {
        display: flex;
        gap: 16px;
        padding: 10px 16px;
        border-bottom: 1px solid #1e293b;
    }

    .match-item:last-child {
        border-bottom: none;
    }

    .line-number {
        flex-shrink: 0;
        font-size: 11px;
        color: #64748b;
        min-width: 60px;
    }

    .match-content {
        flex: 1;
        margin: 0;
        font-family: "JetBrains Mono", "Fira Code", monospace;
        font-size: 12px;
        color: #94a3b8;
        white-space: pre-wrap;
        word-break: break-all;
        overflow-x: auto;
    }

    .match-content :global(mark) {
        background: rgba(251, 191, 36, 0.3);
        color: #fbbf24;
        padding: 1px 2px;
        border-radius: 2px;
    }
</style>
