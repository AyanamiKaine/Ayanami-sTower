<script lang="ts">
    export let title: string;
    export let author: string;
    export let chapter: string = "";
    export let page: number | string = "";
    export let section: string = "";
    export let url: string = "";
    export let notes: string = "";

    // Helper to check if any meta info exists to render the divider line
    $: hasMeta = chapter || section || page;
</script>

<div class="source-card">
    <div class="card-content">
        <header>
            <h4 class="title">{title}</h4>
            <p class="author">By <span>{author}</span></p>
        </header>

        {#if notes}
            <div class="notes-section">
                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="icon"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path><polyline points="14 2 14 8 20 8"></polyline><line x1="16" y1="13" x2="8" y2="13"></line><line x1="16" y1="17" x2="8" y2="17"></line><polyline points="10 9 9 9 8 9"></polyline></svg>
                <p>{notes}</p>
            </div>
        {/if}

        <div class="meta-footer">
            <div class="meta-data">
                {#if chapter}<span class="tag"><strong>Ch.</strong> {chapter}</span>{/if}
                {#if section}<span class="tag"><strong>§</strong> {section}</span>{/if}
                {#if page}<span class="tag"><strong>Pg.</strong> {page}</span>{/if}
            </div>
            
            {#if url}
                <a href={url} class="source-link" target="_blank" rel="noopener noreferrer">
                    Source 
                    <svg xmlns="http://www.w3.org/2000/svg" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6"></path><polyline points="15 3 21 3 21 9"></polyline><line x1="10" y1="14" x2="21" y2="3"></line></svg>
                </a>
            {/if}
        </div>
    </div>
</div>

<style>
    /* CSS Variables for easy theming */
    .source-card {
        --bg-color: #ffffff;
        --border-color: #e5e7eb;
        --accent-color: #6366f1;
        --text-primary: #1f2937;
        --text-secondary: #6b7280;
        --text-muted: #9ca3af;
        
        background-color: var(--bg-color);
        border: 1px solid var(--border-color);
        border-left: 4px solid var(--accent-color);
        border-radius: 6px;
        padding: 1.25rem;
        margin: 1.5rem 0;
        font-family: system-ui, -apple-system, sans-serif;
        box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
    }

    header {
        margin-bottom: 1rem;
    }

    .title {
        margin: 0 0 0.25rem 0;
        font-size: 1.1rem;
        color: var(--text-primary);
        line-height: 1.4;
    }

    .author {
        margin: 0;
        font-size: 0.9rem;
        color: var(--text-secondary);
        text-transform: uppercase;
        letter-spacing: 0.05em;
        font-weight: 500;
    }

    .author span {
        color: var(--text-primary);
        font-weight: 600;
    }

    /* Notes Section */
    .notes-section {
        background: rgba(255, 255, 255, 0.5);
        border-radius: 4px;
        padding: 0.75rem;
        margin-bottom: 1rem;
        display: flex;
        gap: 0.5rem;
        align-items: flex-start;
        font-size: 0.95rem;
        color: var(--text-secondary);
        border: 1px dashed var(--border-color);
    }
    
    .notes-section p {
        margin: 0;
        line-height: 1.5;
    }

    .icon {
        flex-shrink: 0;
        margin-top: 3px;
        color: var(--text-muted);
    }

    /* Footer area: Metadata and Link */
    .meta-footer {
        display: flex;
        justify-content: space-between;
        align-items: center;
        flex-wrap: wrap;
        gap: 1rem;
        padding-top: 0.75rem;
        border-top: 1px solid var(--border-color);
    }

    .meta-data {
        display: flex;
        gap: 0.75rem;
        flex-wrap: wrap;
    }

    .tag {
        font-size: 0.85rem;
        color: var(--text-primary);
        background: #f3f4f6;
        padding: 0.3rem 0.6rem;
        border-radius: 4px;
        border: 1px solid #d1d5db;
        font-weight: 500;
    }

    .source-link {
        display: inline-flex;
        align-items: center;
        gap: 0.25rem;
        font-size: 0.85rem;
        font-weight: 600;
        color: var(--accent-color);
        text-decoration: none;
        transition: opacity 0.2s;
    }

    .source-link:hover {
        opacity: 0.8;
        text-decoration: underline;
    }

    /* Dark Mode */
    @media (prefers-color-scheme: dark) {
        .source-card {
            --bg-color: #1f2937;
            --border-color: #374151;
            --accent-color: #818cf8;
            --text-primary: #f3f4f6;
            --text-secondary: #d1d5db;
            --text-muted: #9ca3af;
        }
        
        .tag {
            background: #374151;
            color: #f3f4f6;
            border-color: #4b5563;
        }
        
        .notes-section {
            background: rgba(0,0,0,0.3);
            border-color: #4b5563;
        }
    }
</style>