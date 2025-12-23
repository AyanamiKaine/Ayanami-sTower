<script lang="ts">
    import FileBrowser from "./FileBrowser.svelte";

    // State
    let activeView = $state<"home" | "browser" | "editor">("home");
    let selectedFilePath = $state<string | null>(null);
    let selectedFileContent = $state<string | null>(null);
    let x4DataPath = $state<string>("");

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

    // Load saved path from localStorage
    $effect(() => {
        if (typeof localStorage !== "undefined") {
            const saved = localStorage.getItem("x4-data-path");
            if (saved) {
                x4DataPath = normalizePath(saved);
            }
        }
    });

    function handleFileSelect(path: string, content: string) {
        selectedFilePath = path;
        selectedFileContent = content;
        // Navigate to editor with the file
        window.location.href = `/editor?file=${encodeURIComponent(path)}`;
    }

    function saveDataPath() {
        const normalized = normalizePath(x4DataPath);
        x4DataPath = normalized;
        if (normalized && typeof localStorage !== "undefined") {
            localStorage.setItem("x4-data-path", normalized);
        }
    }

    function handleBrowseClick() {
        saveDataPath(); // Save before browsing
        activeView = "browser";
    }

    // Derive if path is saved
    let pathSaved = $derived(() => {
        if (typeof localStorage === "undefined") return false;
        const saved = localStorage.getItem("x4-data-path");
        return saved === normalizePath(x4DataPath) && x4DataPath !== "";
    });

    const features = [
        {
            icon: "🌳",
            title: "Visual XML Tree",
            description:
                "Browse complex XML structures with an intuitive tree view. Expand, collapse, and navigate game data easily.",
        },
        {
            icon: "🎯",
            title: "XPath Generation",
            description:
                "Click on any element to auto-generate the correct XPath selector for your diff.xml patches.",
        },
        {
            icon: "📝",
            title: "Diff.xml Builder",
            description:
                "Create add, replace, and remove operations visually. No more syntax errors in your patches.",
        },
        {
            icon: "📦",
            title: "Mod Workspace",
            description:
                "Create and manage mod projects. Save diff.xml files directly to the correct folder structure.",
        },
        {
            icon: "📁",
            title: "File Browser",
            description:
                "Point to your unpacked X4 data directory and browse game files directly.",
        },
        {
            icon: "🔍",
            title: "Database Search",
            description:
                "Search across all XML files to find factions, ships, wares, macros, and more.",
        },
    ];

    const quickLinks = [
        {
            name: "libraries/",
            description: "Ship and station components",
            icon: "🚀",
        },
        { name: "md/", description: "Mission director scripts", icon: "📜" },
        { name: "aiscripts/", description: "AI behavior scripts", icon: "🤖" },
        { name: "t/", description: "Text and localization", icon: "🌐" },
        { name: "maps/", description: "Universe and sector data", icon: "🗺️" },
        { name: "assets/", description: "Game assets definitions", icon: "📦" },
    ];
</script>

<div class="homepage">
    <!-- Hero Section -->
    <header class="hero">
        <div class="hero-content">
            <div class="logo">
                <span class="logo-icon">🛠️</span>
                <h1>X4 Modding Studio</h1>
            </div>
            <p class="tagline">
                Visual diff.xml patch editor for X4: Foundations
            </p>
            <p class="subtitle">
                Create game modifications without writing XPath by hand
            </p>

            <div class="hero-actions">
                <a href="/editor" class="btn btn-primary btn-large">
                    <span>✨</span> Open Editor
                </a>
                <a href="/workspace" class="btn btn-secondary btn-large">
                    <span>📦</span> Mod Workspace
                </a>
                <button
                    class="btn btn-secondary btn-large"
                    onclick={() => (activeView = "browser")}
                >
                    <span>📂</span> Browse Files
                </button>
                <a href="/database" class="btn btn-secondary btn-large">
                    <span>🔍</span> Search Database
                </a>
            </div>
        </div>

        <div class="hero-visual">
            <div class="code-preview">
                <div class="code-header">
                    <span class="dot red"></span>
                    <span class="dot yellow"></span>
                    <span class="dot green"></span>
                    <span class="filename">diff.xml</span>
                </div>
                <pre class="code-content"><code
                        >&lt;?xml version="1.0" encoding="utf-8"?&gt;
&lt;diff&gt;
  &lt;replace sel="//faction[@id='argon']/relation[@faction='paranid']/@relation"&gt;
    0.8
  &lt;/replace&gt;
  &lt;add sel="//faction[@id='argon']"&gt;
    &lt;relation faction="terran" relation="0.5"/&gt;
  &lt;/add&gt;
&lt;/diff&gt;</code
                    ></pre>
            </div>
        </div>
    </header>

    <!-- Quick Setup Section -->
    <section class="setup-section">
        <div class="setup-card">
            <h2>🎮 Quick Setup</h2>
            <p>Point to your unpacked X4 data directory to browse game files</p>

            <div class="path-setup">
                <input
                    type="text"
                    class="path-input"
                    placeholder="/path/to/X4/unpacked/data"
                    bind:value={x4DataPath}
                    onblur={saveDataPath}
                    onkeydown={(e) => e.key === "Enter" && handleBrowseClick()}
                />
                <button
                    class="btn btn-primary"
                    onclick={handleBrowseClick}
                    disabled={!x4DataPath}
                >
                    Browse
                </button>
            </div>

            {#if pathSaved()}
                <div class="path-saved-indicator">
                    <span>✓</span> Path saved - will be remembered next time
                </div>
            {/if}

            <div class="setup-hint">
                <span>💡</span>
                <span>Extract X4 data using: <code>X4Unpacker</code></span>
            </div>
        </div>
    </section>

    <!-- Features Grid -->
    <section class="features-section">
        <h2>Features</h2>
        <div class="features-grid">
            {#each features as feature}
                <div class="feature-card">
                    <span class="feature-icon">{feature.icon}</span>
                    <h3>{feature.title}</h3>
                    <p>{feature.description}</p>
                </div>
            {/each}
        </div>
    </section>

    <!-- Quick Links Section -->
    {#if x4DataPath}
        <section class="quicklinks-section">
            <h2>Common Directories</h2>
            <div class="quicklinks-grid">
                {#each quickLinks as link}
                    <a
                        href={`/browser?path=${encodeURIComponent(x4DataPath + "/" + link.name)}`}
                        class="quicklink-card"
                    >
                        <span class="quicklink-icon">{link.icon}</span>
                        <div class="quicklink-info">
                            <span class="quicklink-name">{link.name}</span>
                            <span class="quicklink-desc"
                                >{link.description}</span
                            >
                        </div>
                    </a>
                {/each}
            </div>
        </section>
    {/if}

    <!-- Getting Started Section -->
    <section class="guide-section">
        <h2>Getting Started</h2>
        <div class="steps">
            <div class="step">
                <div class="step-number">1</div>
                <div class="step-content">
                    <h3>Extract Game Data</h3>
                    <p>
                        Use XRCatTool to extract X4's .cat files to a folder you
                        can browse.
                    </p>
                </div>
            </div>
            <div class="step">
                <div class="step-number">2</div>
                <div class="step-content">
                    <h3>Open XML File</h3>
                    <p>
                        Browse to the XML file you want to modify, like
                        factions.xml or wares.xml.
                    </p>
                </div>
            </div>
            <div class="step">
                <div class="step-number">3</div>
                <div class="step-content">
                    <h3>Select & Modify</h3>
                    <p>
                        Right-click elements to add, replace, or remove them.
                        The XPath is generated automatically.
                    </p>
                </div>
            </div>
            <div class="step">
                <div class="step-number">4</div>
                <div class="step-content">
                    <h3>Export diff.xml</h3>
                    <p>
                        Copy the generated diff.xml to your mod's folder and
                        you're done!
                    </p>
                </div>
            </div>
        </div>
    </section>

    <!-- Footer -->
    <footer class="footer">
        <p>
            X4 Modding Studio • Built for the X4: Foundations modding community
        </p>
        <p class="footer-links">
            <a
                href="https://www.egosoft.com/games/x4/info_en.php"
                target="_blank">Egosoft</a
            >
            <span>•</span>
            <a
                href="https://wiki.egosoft.com:1337/X4%20Foundations%20Wiki"
                target="_blank">X4 Wiki</a
            >
            <span>•</span>
            <a
                href="https://forum.egosoft.com/viewforum.php?f=181"
                target="_blank">Modding Forum</a
            >
        </p>
    </footer>
</div>

<!-- File Browser Modal -->
{#if activeView === "browser"}
    <!-- svelte-ignore a11y_no_static_element_interactions -->
    <!-- svelte-ignore a11y_click_events_have_key_events -->
    <div
        class="modal-overlay"
        onclick={() => (activeView = "home")}
        role="dialog"
        aria-modal="true"
        aria-label="File Browser"
        tabindex="-1"
    >
        <!-- svelte-ignore a11y_no_static_element_interactions -->
        <!-- svelte-ignore a11y_click_events_have_key_events -->
        <div class="modal-content" onclick={(e) => e.stopPropagation()}>
            <div class="modal-header">
                <h2>📂 Browse X4 Data</h2>
                <button class="btn-close" onclick={() => (activeView = "home")}
                    >×</button
                >
            </div>
            <div class="modal-body">
                <FileBrowser
                    initialPath={x4DataPath}
                    onFileSelect={handleFileSelect}
                />
            </div>
        </div>
    </div>
{/if}

<style>
    .homepage {
        min-height: 100vh;
        background: linear-gradient(180deg, #0f172a 0%, #1e293b 100%);
        color: #e2e8f0;
    }

    /* Hero Section */
    .hero {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 48px;
        padding: 64px 48px;
        max-width: 1400px;
        margin: 0 auto;
        align-items: center;
    }

    @media (max-width: 1024px) {
        .hero {
            grid-template-columns: 1fr;
            text-align: center;
        }

        .hero-visual {
            display: none;
        }
    }

    .logo {
        display: flex;
        align-items: center;
        gap: 16px;
        margin-bottom: 16px;
    }

    @media (max-width: 1024px) {
        .logo {
            justify-content: center;
        }
    }

    .logo-icon {
        font-size: 48px;
    }

    .logo h1 {
        margin: 0;
        font-size: 36px;
        font-weight: 700;
        background: linear-gradient(135deg, #67e8f9 0%, #3b82f6 100%);
        -webkit-background-clip: text;
        -webkit-text-fill-color: transparent;
        background-clip: text;
    }

    .tagline {
        font-size: 24px;
        color: #e2e8f0;
        margin: 0 0 8px 0;
    }

    .subtitle {
        font-size: 16px;
        color: #94a3b8;
        margin: 0 0 32px 0;
    }

    .hero-actions {
        display: flex;
        gap: 16px;
    }

    @media (max-width: 1024px) {
        .hero-actions {
            justify-content: center;
        }
    }

    .btn {
        display: inline-flex;
        align-items: center;
        gap: 8px;
        padding: 12px 24px;
        border-radius: 8px;
        font-size: 16px;
        font-weight: 600;
        cursor: pointer;
        transition: all 0.2s;
        border: none;
        text-decoration: none;
    }

    .btn-large {
        padding: 16px 32px;
        font-size: 18px;
    }

    .btn-primary {
        background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%);
        color: white;
        box-shadow: 0 4px 14px rgba(59, 130, 246, 0.4);
    }

    .btn-primary:hover:not(:disabled) {
        transform: translateY(-2px);
        box-shadow: 0 6px 20px rgba(59, 130, 246, 0.5);
    }

    .btn-primary:disabled {
        opacity: 0.5;
        cursor: not-allowed;
    }

    .btn-secondary {
        background: rgba(255, 255, 255, 0.1);
        color: #e2e8f0;
        border: 1px solid rgba(255, 255, 255, 0.2);
    }

    .btn-secondary:hover {
        background: rgba(255, 255, 255, 0.15);
    }

    /* Code Preview */
    .hero-visual {
        display: flex;
        justify-content: center;
    }

    .code-preview {
        background: #1e293b;
        border-radius: 12px;
        overflow: hidden;
        box-shadow: 0 20px 40px rgba(0, 0, 0, 0.3);
        border: 1px solid #334155;
        max-width: 500px;
    }

    .code-header {
        display: flex;
        align-items: center;
        gap: 8px;
        padding: 12px 16px;
        background: #0f172a;
        border-bottom: 1px solid #334155;
    }

    .dot {
        width: 12px;
        height: 12px;
        border-radius: 50%;
    }

    .dot.red {
        background: #ef4444;
    }
    .dot.yellow {
        background: #eab308;
    }
    .dot.green {
        background: #22c55e;
    }

    .filename {
        margin-left: 8px;
        font-size: 13px;
        color: #94a3b8;
    }

    .code-content {
        margin: 0;
        padding: 20px;
        font-size: 13px;
        line-height: 1.6;
        color: #94a3b8;
        overflow-x: auto;
    }

    .code-content code {
        color: #67e8f9;
    }

    /* Setup Section */
    .setup-section {
        padding: 0 48px 64px;
        max-width: 1400px;
        margin: 0 auto;
    }

    .setup-card {
        background: rgba(255, 255, 255, 0.05);
        border: 1px solid #334155;
        border-radius: 16px;
        padding: 32px;
        text-align: center;
    }

    .setup-card h2 {
        margin: 0 0 8px 0;
        font-size: 24px;
    }

    .setup-card > p {
        margin: 0 0 24px 0;
        color: #94a3b8;
    }

    .path-setup {
        display: flex;
        gap: 12px;
        max-width: 600px;
        margin: 0 auto;
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

    .setup-hint {
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 8px;
        margin-top: 16px;
        font-size: 13px;
        color: #64748b;
    }

    .setup-hint code {
        background: #0f172a;
        padding: 4px 8px;
        border-radius: 4px;
        color: #a5b4fc;
    }

    .path-saved-indicator {
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 6px;
        margin-top: 12px;
        font-size: 13px;
        color: #22c55e;
    }

    .path-saved-indicator span {
        font-weight: 600;
    }

    /* Features Section */
    .features-section {
        padding: 64px 48px;
        max-width: 1400px;
        margin: 0 auto;
    }

    .features-section h2 {
        text-align: center;
        margin: 0 0 48px 0;
        font-size: 28px;
    }

    .features-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
        gap: 24px;
    }

    .feature-card {
        background: rgba(255, 255, 255, 0.03);
        border: 1px solid #334155;
        border-radius: 12px;
        padding: 24px;
        transition: all 0.2s;
    }

    .feature-card:hover {
        background: rgba(255, 255, 255, 0.05);
        transform: translateY(-4px);
    }

    .feature-icon {
        font-size: 32px;
        display: block;
        margin-bottom: 16px;
    }

    .feature-card h3 {
        margin: 0 0 8px 0;
        font-size: 18px;
        color: #f8fafc;
    }

    .feature-card p {
        margin: 0;
        font-size: 14px;
        color: #94a3b8;
        line-height: 1.6;
    }

    /* Quick Links Section */
    .quicklinks-section {
        padding: 0 48px 64px;
        max-width: 1400px;
        margin: 0 auto;
    }

    .quicklinks-section h2 {
        text-align: center;
        margin: 0 0 32px 0;
        font-size: 24px;
    }

    .quicklinks-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
        gap: 16px;
    }

    .quicklink-card {
        display: flex;
        align-items: center;
        gap: 12px;
        padding: 16px;
        background: rgba(255, 255, 255, 0.03);
        border: 1px solid #334155;
        border-radius: 8px;
        text-decoration: none;
        color: inherit;
        transition: all 0.2s;
    }

    .quicklink-card:hover {
        background: rgba(59, 130, 246, 0.1);
        border-color: #3b82f6;
    }

    .quicklink-icon {
        font-size: 24px;
    }

    .quicklink-info {
        display: flex;
        flex-direction: column;
    }

    .quicklink-name {
        font-weight: 600;
        color: #67e8f9;
    }

    .quicklink-desc {
        font-size: 12px;
        color: #64748b;
    }

    /* Guide Section */
    .guide-section {
        padding: 64px 48px;
        max-width: 1000px;
        margin: 0 auto;
    }

    .guide-section h2 {
        text-align: center;
        margin: 0 0 48px 0;
        font-size: 28px;
    }

    .steps {
        display: flex;
        flex-direction: column;
        gap: 24px;
    }

    .step {
        display: flex;
        gap: 24px;
        align-items: flex-start;
    }

    .step-number {
        flex-shrink: 0;
        width: 48px;
        height: 48px;
        display: flex;
        align-items: center;
        justify-content: center;
        background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%);
        border-radius: 50%;
        font-size: 20px;
        font-weight: 700;
    }

    .step-content h3 {
        margin: 0 0 8px 0;
        font-size: 18px;
        color: #f8fafc;
    }

    .step-content p {
        margin: 0;
        color: #94a3b8;
        line-height: 1.6;
    }

    /* Footer */
    .footer {
        padding: 48px;
        text-align: center;
        border-top: 1px solid #334155;
        color: #64748b;
    }

    .footer p {
        margin: 0 0 8px 0;
    }

    .footer-links {
        display: flex;
        justify-content: center;
        gap: 16px;
    }

    .footer-links a {
        color: #94a3b8;
        text-decoration: none;
        transition: color 0.2s;
    }

    .footer-links a:hover {
        color: #3b82f6;
    }

    /* Modal */
    .modal-overlay {
        position: fixed;
        inset: 0;
        background: rgba(0, 0, 0, 0.8);
        display: flex;
        align-items: center;
        justify-content: center;
        z-index: 1000;
        padding: 24px;
    }

    .modal-content {
        background: #1e293b;
        border-radius: 16px;
        width: 100%;
        max-width: 900px;
        max-height: 80vh;
        display: flex;
        flex-direction: column;
        overflow: hidden;
    }

    .modal-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        padding: 16px 24px;
        border-bottom: 1px solid #334155;
    }

    .modal-header h2 {
        margin: 0;
        font-size: 18px;
    }

    .btn-close {
        background: none;
        border: none;
        color: #94a3b8;
        font-size: 24px;
        cursor: pointer;
        padding: 0;
        line-height: 1;
    }

    .btn-close:hover {
        color: #e2e8f0;
    }

    .modal-body {
        flex: 1;
        overflow: hidden;
        display: flex;
        flex-direction: column;
    }
</style>
