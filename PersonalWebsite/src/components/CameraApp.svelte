<script>
    import { onMount, onDestroy } from "svelte";

    let video;
    let canvas;
    let stream;
    let facingMode = "environment";
    let error = null;
    let torchSupported = false;
    let torchEnabled = false;
    let pendingUploads = 0;
    let isFlashing = false;
    let lastUploadStatus = "";

    async function startCamera() {
        try {
            if (stream) {
                stream.getTracks().forEach((track) => track.stop());
            }

            try {
                stream = await navigator.mediaDevices.getUserMedia({
                    video: {
                        facingMode: facingMode,
                        width: { ideal: 4096 },
                        height: { ideal: 2160 },
                    },
                });
            } catch (e) {
                console.warn(
                    "Specific facingMode failed, falling back to any video camera",
                    e,
                );
                stream = await navigator.mediaDevices.getUserMedia({
                    video: {
                        width: { ideal: 4096 },
                        height: { ideal: 2160 },
                    },
                });
            }

            video.srcObject = stream;
            video.play();

            // Check for torch support
            const track = stream.getVideoTracks()[0];
            const capabilities = track.getCapabilities();
            torchSupported = !!capabilities.torch;
            torchEnabled = false; // Reset state

            error = null;
        } catch (err) {
            console.error("Error accessing camera:", err);
            error =
                "Could not access the camera. Please ensure you have granted permissions.";
        }
    }

    function switchCamera() {
        facingMode = facingMode === "user" ? "environment" : "user";
        startCamera();
    }

    async function toggleTorch() {
        if (!stream) return;
        const track = stream.getVideoTracks()[0];

        try {
            await track.applyConstraints({
                advanced: [{ torch: !torchEnabled }],
            });
            torchEnabled = !torchEnabled;
        } catch (err) {
            console.error("Error toggling torch:", err);
        }
    }

    async function uploadBlob(blob) {
        pendingUploads++;
        lastUploadStatus = "Uploading...";

        const formData = new FormData();
        formData.append("file", blob, `capture-${Date.now()}.jpg`);

        const apiUrl = "https://api.ayanamikaine.com/upload";
        const password = "SecretPassword123";

        try {
            const response = await fetch(apiUrl, {
                method: "POST",
                headers: {
                    "X-Password": password,
                },
                body: formData,
            });

            if (response.ok) {
                lastUploadStatus = "Upload successful";
                console.log("Image uploaded successfully");
            } else {
                console.error(
                    "Upload failed:",
                    response.status,
                    response.statusText,
                );
                lastUploadStatus = `Failed: ${response.status}`;
                error = `Upload failed: ${response.statusText}`;
            }
        } catch (err) {
            console.error("Error uploading:", err);
            lastUploadStatus = "Network Error";
            error = "Network error during upload";
        } finally {
            pendingUploads--;
            if (pendingUploads === 0) {
                setTimeout(() => {
                    if (pendingUploads === 0) lastUploadStatus = "";
                }, 2000);
            }
        }
    }

    function triggerFlash() {
        isFlashing = true;
        setTimeout(() => {
            isFlashing = false;
        }, 150);
    }

    function capturePhoto() {
        if (!video || !canvas) return;

        triggerFlash();

        const context = canvas.getContext("2d");
        canvas.width = video.videoWidth;
        canvas.height = video.videoHeight;
        context.drawImage(video, 0, 0, canvas.width, canvas.height);

        canvas.toBlob(
            (blob) => {
                if (blob) {
                    // Fire and forget (it handles its own state)
                    uploadBlob(blob);
                }
            },
            "image/jpeg",
            0.85,
        ); // High quality JPEG
    }

    onMount(() => {
        startCamera();
    });

    onDestroy(() => {
        if (stream) {
            stream.getTracks().forEach((track) => track.stop());
        }
    });
</script>

<div class="camera-app">
    {#if error}
        <div class="error-message">
            <p>{error}</p>
            <div class="error-actions">
                <button on:click={() => (error = null)} class="btn secondary"
                    >Dismiss</button
                >
                <button on:click={startCamera} class="btn primary"
                    >Restart Camera</button
                >
            </div>
        </div>
    {/if}

    <div class="viewfinder">
        <!-- svelte-ignore a11y-media-has-caption -->
        <video bind:this={video} playsinline muted></video>
        <canvas bind:this={canvas} style="display: none;"></canvas>

        {#if isFlashing}
            <div class="flash-overlay"></div>
        {/if}

        {#if pendingUploads > 0 || lastUploadStatus}
            <div class="status-indicator">
                {#if pendingUploads > 0}
                    <span class="spinner"></span>
                    {pendingUploads} sending...
                {:else}
                    {lastUploadStatus}
                {/if}
            </div>
        {/if}

        <div class="controls">
            <button
                on:click={switchCamera}
                class="btn icon-btn"
                title="Switch Camera"
                aria-label="Switch Camera"
            >
                <svg
                    xmlns="http://www.w3.org/2000/svg"
                    width="24"
                    height="24"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    stroke-width="2"
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    ><path
                        d="M20 5H9l-7 7 7 7h11a2 2 0 0 0 2-2V7a2 2 0 0 0-2-2Z"
                    /><line x1="18" y1="9" x2="12" y2="15" /><line
                        x1="12"
                        y1="9"
                        x2="18"
                        y2="15"
                    /></svg
                >
            </button>

            {#if torchSupported}
                <button
                    on:click={toggleTorch}
                    class="btn icon-btn"
                    class:active={torchEnabled}
                    title={torchEnabled ? "Turn off flash" : "Turn on flash"}
                >
                    {#if torchEnabled}
                        <svg
                            xmlns="http://www.w3.org/2000/svg"
                            width="24"
                            height="24"
                            viewBox="0 0 24 24"
                            fill="none"
                            stroke="currentColor"
                            stroke-width="2"
                            stroke-linecap="round"
                            stroke-linejoin="round"
                            ><path
                                d="M15 14c.2-1 .7-1.7 1.5-2.5 1-.9 1.5-2.2 1.5-3.5A6 6 0 0 0 6 8c0 1 .2 2.2 1.5 3.5.7.7 1.3 1.5 1.5 2.5"
                            /><path d="M9 18h6" /><path d="M10 22h4" /></svg
                        >
                    {:else}
                        <svg
                            xmlns="http://www.w3.org/2000/svg"
                            width="24"
                            height="24"
                            viewBox="0 0 24 24"
                            fill="none"
                            stroke="currentColor"
                            stroke-width="2"
                            stroke-linecap="round"
                            stroke-linejoin="round"
                            ><path
                                d="M15 14c.2-1 .7-1.7 1.5-2.5 1-.9 1.5-2.2 1.5-3.5A6 6 0 0 0 6 8c0 1 .2 2.2 1.5 3.5.7.7 1.3 1.5 1.5 2.5"
                            /><path d="M9 18h6" /><path d="M10 22h4" /><line
                                x1="4"
                                x2="20"
                                y1="2"
                                y2="22"
                            /></svg
                        >
                    {/if}
                </button>
            {:else}
                <div class="spacer"></div>
            {/if}

            <button
                on:click={capturePhoto}
                class="capture-btn"
                aria-label="Take Photo"
            ></button>

            {#if !torchSupported}
                <div class="spacer"></div>
            {/if}
        </div>
    </div>
</div>

<style>
    .camera-app {
        position: fixed;
        top: 0;
        left: 0;
        width: 100vw;
        height: 100vh;
        background-color: #000;
        color: white;
        display: flex;
        flex-direction: column;
        z-index: 9999; /* Ensure it stays on top */
    }

    .error-message {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        height: 100%;
        padding: 2rem;
        text-align: center;
        z-index: 20;
        background: #000;
    }

    .error-actions {
        display: flex;
        gap: 1rem;
        margin-top: 1.5rem;
    }

    .viewfinder {
        position: relative;
        width: 100%;
        height: 100%;
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        overflow: hidden;
    }

    video {
        width: 100%;
        height: 100%;
        object-fit: cover;
    }

    .flash-overlay {
        position: absolute;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        background-color: white;
        z-index: 10;
        pointer-events: none;
        opacity: 0;
        animation: flash 150ms ease-out;
    }

    @keyframes flash {
        0% {
            opacity: 0.8;
        }
        100% {
            opacity: 0;
        }
    }

    .status-indicator {
        position: absolute;
        top: env(safe-area-inset-top, 20px);
        margin-top: 1rem;
        left: 50%;
        transform: translateX(-50%);
        background: rgba(0, 0, 0, 0.6);
        color: white;
        padding: 0.5rem 1rem;
        border-radius: 20px;
        z-index: 5;
        font-size: 0.9rem;
        display: flex;
        align-items: center;
        gap: 0.6rem;
        backdrop-filter: blur(4px);
        -webkit-backdrop-filter: blur(4px);
    }

    .spinner {
        width: 12px;
        height: 12px;
        border: 2px solid rgba(255, 255, 255, 0.3);
        border-top-color: white;
        border-radius: 50%;
        animation: spin 1s linear infinite;
    }

    @keyframes spin {
        to {
            transform: rotate(360deg);
        }
    }

    .controls {
        position: absolute;
        bottom: 2rem;
        left: 50%;
        transform: translateX(-50%);
        width: calc(100% - 3rem);
        max-width: 400px;
        padding: 1.25rem 1.5rem;
        background: rgba(0, 0, 0, 0.5);
        backdrop-filter: blur(20px);
        -webkit-backdrop-filter: blur(20px);
        border-radius: 2rem;
        display: flex;
        justify-content: space-around;
        align-items: center;
        gap: 1rem;
        margin-bottom: env(safe-area-inset-bottom, 0px);
        z-index: 5;
    }

    .btn {
        background: rgba(255, 255, 255, 0.2);
        border: none;
        color: white;
        padding: 0.8rem 1.5rem;
        border-radius: 2rem;
        font-weight: 600;
        backdrop-filter: blur(10px);
        cursor: pointer;
        font-size: 1rem;
        transition: background 0.2s;
    }

    .btn.active {
        background: rgba(255, 255, 255, 0.8);
        color: black;
    }

    .btn:hover {
        background: rgba(255, 255, 255, 0.3);
    }

    .btn.primary {
        background: white;
        color: black;
    }

    .btn.secondary {
        background: rgba(255, 255, 255, 0.2);
    }

    .btn.icon-btn {
        padding: 0.8rem;
        border-radius: 50%;
        display: flex;
        align-items: center;
        justify-content: center;
    }

    .capture-btn {
        width: 72px;
        height: 72px;
        border-radius: 50%;
        border: 4px solid white;
        background: transparent;
        position: relative;
        cursor: pointer;
        padding: 0;
    }

    .capture-btn::after {
        content: "";
        position: absolute;
        top: 50%;
        left: 50%;
        transform: translate(-50%, -50%);
        width: 60px;
        height: 60px;
        background: white;
        border-radius: 50%;
        transition: transform 0.1s;
    }

    .capture-btn:active::after {
        transform: translate(-50%, -50%) scale(0.9);
        background: #eee;
    }

    .spacer {
        width: 40px; /* Balance the layout for icon button */
    }
</style>
