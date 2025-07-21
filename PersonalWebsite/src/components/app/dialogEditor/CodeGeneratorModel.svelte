<script>
    export let generatedCode  = '';

    export let onClose = () => {};
</script>

<div class="modal-backdrop" on:click={onClose} role="dialog" aria-modal="true">
    <div class="modal-content" on:click|stopPropagation>
        <header class="modal-header">
            <h2>Generated Source Code</h2>
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