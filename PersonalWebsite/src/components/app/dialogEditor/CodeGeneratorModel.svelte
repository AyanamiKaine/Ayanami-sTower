<script>
    export let generatedCode  = '';
    let copied = false;

    export let onClose = () => {};

    async function copyCode() {
            if (!generatedCode) return;

            try {
                await navigator.clipboard.writeText(generatedCode);
                copied = true; // Set state to show feedback
                
                // Reset the feedback message after 2 seconds
                setTimeout(() => {
                    copied = false;
                }, 2000);

            } catch (err) {
                console.error('Failed to copy text: ', err);
                // Optionally, you could show an error notification here
            }
        }

</script>

<div class="modal-backdrop" on:click={onClose} role="dialog" aria-modal="true">
    <div class="modal-content" on:click|stopPropagation>
        <header class="modal-header">
            <h2>Generated Source Code</h2>
               <button class="copy-btn" on:click={copyCode} title="Copy code to clipboard">
                {#if copied}
                    <span>✅ Copied!</span>
                {:else}
                    <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect><path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path></svg>
                    <span>Copy</span>
                {/if}
            </button>
            <button on:click={onClose} class="close-btn" aria-label="Close modal">&times;</button>
        </header>
        <main class="code-container">
            <pre><code>{generatedCode }</code></pre>
        </main>
    </div>
</div>

<style>
    .modal-backdrop {
        position: fixed;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        background-color: rgba(0, 0, 0, 0.6);
        display: flex;
        justify-content: center;
        align-items: center;
        z-index: 1000;
    }
    .modal-content {
        background: #fff;
        border-radius: 8px;
        box-shadow: 0 5px 15px rgba(0,0,0,0.3);
        width: 80%;
        max-width: 800px;
        max-height: 80vh;
        display: flex;
        flex-direction: column;
    }
    .modal-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 1rem 1.5rem;
        border-bottom: 1px solid #eee;
    }
    .modal-header h2 {
        margin: 0;
        font-size: 1.25rem;
    }
    .copy-btn {
        display: inline-flex;
        align-items: center;
        gap: 0.5rem;
        background-color: #f3f4f6;
        border: 1px solid #d1d5db;
        border-radius: 6px;
        padding: 6px 12px;
        font-size: 0.875rem;
        font-weight: 500;
        cursor: pointer;
        transition: background-color 0.2s;
        white-space: nowrap;
    }
    .copy-btn:hover {
        background-color: #e5e7eb;
    }
    .copy-btn:active {
        background-color: #d1d5db;
    }
    .close-btn {
        background: none;
        border: none;
        font-size: 2rem;
        cursor: pointer;
        line-height: 1;
        padding: 0;
        color: #888;
    }
    .close-btn:hover {
        color: #000;
    }
    .code-container {
        padding: 1.5rem;
        overflow-y: auto;
        background: #2d2d2d;
        color: #f1f1f1;
        font-family: 'Courier New', Courier, monospace;
        font-size: 0.9rem;
        flex-grow: 1;
    }
    pre {
        margin: 0;
        white-space: pre-wrap; /* Allows text to wrap */
    }
</style>