<script lang="ts">
    interface Props {
        visible: boolean;
        title: string;
        onClose?: () => void;
        onConfirm?: () => void;
        confirmText?: string;
        confirmDisabled?: boolean;
    }

    let {
        visible,
        title,
        onClose,
        onConfirm,
        confirmText = "Confirm",
        confirmDisabled = false,
        children,
    }: Props & { children?: any } = $props();

    function handleKeyDown(e: KeyboardEvent) {
        if (e.key === "Escape") {
            onClose?.();
        }
    }

    function handleBackdropClick(e: MouseEvent) {
        if (e.target === e.currentTarget) {
            onClose?.();
        }
    }

    $effect(() => {
        if (visible) {
            document.addEventListener("keydown", handleKeyDown);
        }
        return () => {
            document.removeEventListener("keydown", handleKeyDown);
        };
    });
</script>

{#if visible}
    <!-- svelte-ignore a11y_no_noninteractive_element_interactions -->
    <div
        class="modal-backdrop"
        onclick={handleBackdropClick}
        onkeydown={handleKeyDown}
        role="dialog"
        aria-modal="true"
        tabindex="-1"
    >
        <div class="modal-container">
            <div class="modal-header">
                <h2 class="modal-title">{title}</h2>
                <button class="close-btn" onclick={onClose} aria-label="Close">
                    ✕
                </button>
            </div>

            <div class="modal-body">
                {@render children?.()}
            </div>

            <div class="modal-footer">
                <button class="btn btn-secondary" onclick={onClose}>
                    Cancel
                </button>
                <button
                    class="btn btn-primary"
                    onclick={onConfirm}
                    disabled={confirmDisabled}
                >
                    {confirmText}
                </button>
            </div>
        </div>
    </div>
{/if}

<style>
    .modal-backdrop {
        position: fixed;
        inset: 0;
        z-index: 1000;
        display: flex;
        align-items: center;
        justify-content: center;
        background: rgba(0, 0, 0, 0.7);
        backdrop-filter: blur(4px);
    }

    .modal-container {
        width: 90%;
        max-width: 600px;
        max-height: 80vh;
        background: #1e293b;
        border: 1px solid #334155;
        border-radius: 12px;
        box-shadow: 0 20px 60px rgba(0, 0, 0, 0.5);
        display: flex;
        flex-direction: column;
    }

    .modal-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        padding: 16px 20px;
        border-bottom: 1px solid #334155;
    }

    .modal-title {
        margin: 0;
        font-size: 18px;
        font-weight: 600;
        color: #e2e8f0;
    }

    .close-btn {
        display: flex;
        align-items: center;
        justify-content: center;
        width: 32px;
        height: 32px;
        padding: 0;
        background: none;
        border: none;
        border-radius: 6px;
        cursor: pointer;
        color: #64748b;
        font-size: 16px;
        transition: all 0.15s;
    }

    .close-btn:hover {
        background: rgba(255, 255, 255, 0.1);
        color: #e2e8f0;
    }

    .modal-body {
        flex: 1;
        overflow-y: auto;
        padding: 20px;
    }

    .modal-footer {
        display: flex;
        justify-content: flex-end;
        gap: 12px;
        padding: 16px 20px;
        border-top: 1px solid #334155;
    }

    .btn {
        padding: 8px 16px;
        border-radius: 6px;
        font-size: 14px;
        font-weight: 500;
        cursor: pointer;
        transition: all 0.15s;
    }

    .btn:disabled {
        opacity: 0.5;
        cursor: not-allowed;
    }

    .btn-secondary {
        background: #334155;
        border: 1px solid #475569;
        color: #e2e8f0;
    }

    .btn-secondary:hover:not(:disabled) {
        background: #475569;
    }

    .btn-primary {
        background: #3b82f6;
        border: 1px solid #2563eb;
        color: white;
    }

    .btn-primary:hover:not(:disabled) {
        background: #2563eb;
    }
</style>
