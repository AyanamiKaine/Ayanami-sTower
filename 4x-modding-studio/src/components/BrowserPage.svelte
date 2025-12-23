<script lang="ts">
    import FileBrowser from "./FileBrowser.svelte";

    interface Props {
        urlPath?: string;
    }

    let { urlPath = "" }: Props = $props();

    // Determine the initial path: URL param takes priority, otherwise use saved path
    let initialPath = $state("");
    let isReady = $state(false);

    $effect(() => {
        if (typeof localStorage !== "undefined") {
            if (urlPath) {
                // URL path provided - use it
                initialPath = urlPath;
            } else {
                // No URL path - check localStorage for saved path
                const saved = localStorage.getItem("x4-data-path");
                if (saved) {
                    initialPath = saved;
                }
            }
            isReady = true;
        }
    });

    function handleFileSelect(path: string, content: string) {
        // Navigate to editor with the file
        window.location.href = `/editor?file=${encodeURIComponent(path)}`;
    }
</script>

<div class="browser-page">
    <header class="browser-header">
        <a href="/" class="back-link">
            <span>←</span> Back to Home
        </a>
        <h1>📂 X4 Data Browser</h1>
        {#if initialPath}
            <span class="current-root" title={initialPath}>
                📁 {initialPath.split("/").pop() || initialPath}
            </span>
        {/if}
    </header>
    <div class="browser-content">
        {#if isReady}
            {#if initialPath}
                <FileBrowser {initialPath} onFileSelect={handleFileSelect} />
            {:else}
                <div class="no-path-message">
                    <div class="no-path-icon">📂</div>
                    <h2>No X4 Data Path Set</h2>
                    <p>
                        Please set your X4 unpacked data directory on the home
                        page first.
                    </p>
                    <a href="/" class="btn btn-primary">
                        <span>🏠</span> Go to Home Page
                    </a>
                </div>
            {/if}
        {:else}
            <div class="loading">Loading...</div>
        {/if}
    </div>
</div>

<style>
    .browser-page {
        display: flex;
        flex-direction: column;
        height: 100vh;
        background: #0f172a;
    }

    .browser-header {
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

    .browser-header h1 {
        margin: 0;
        font-size: 20px;
        color: #e2e8f0;
    }

    .current-root {
        margin-left: auto;
        padding: 6px 12px;
        background: rgba(59, 130, 246, 0.1);
        border: 1px solid rgba(59, 130, 246, 0.3);
        border-radius: 6px;
        font-size: 13px;
        color: #67e8f9;
        max-width: 300px;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
    }

    .browser-content {
        flex: 1;
        overflow: hidden;
    }

    .no-path-message {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        height: 100%;
        text-align: center;
        color: #94a3b8;
        gap: 16px;
    }

    .no-path-icon {
        font-size: 64px;
        opacity: 0.5;
    }

    .no-path-message h2 {
        margin: 0;
        font-size: 24px;
        color: #e2e8f0;
    }

    .no-path-message p {
        margin: 0;
        max-width: 400px;
    }

    .btn {
        display: inline-flex;
        align-items: center;
        gap: 8px;
        padding: 12px 24px;
        border-radius: 8px;
        font-size: 14px;
        font-weight: 500;
        text-decoration: none;
        cursor: pointer;
        transition: all 0.2s;
        border: none;
    }

    .btn-primary {
        background: linear-gradient(135deg, #3b82f6, #8b5cf6);
        color: white;
    }

    .btn-primary:hover {
        transform: translateY(-2px);
        box-shadow: 0 8px 20px rgba(59, 130, 246, 0.3);
    }

    .loading {
        display: flex;
        align-items: center;
        justify-content: center;
        height: 100%;
        color: #94a3b8;
        font-size: 16px;
    }
</style>
