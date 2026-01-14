<script>
    import { onMount, onDestroy } from 'svelte';
  
    let video;
    let canvas;
    let stream;
    let facingMode = 'environment';
    let error = null;
    let capturedImage = null;
  
    async function startCamera() {
      try {
        if (stream) {
          stream.getTracks().forEach(track => track.stop());
        }
        
        stream = await navigator.mediaDevices.getUserMedia({
          video: { facingMode: facingMode }
        });
        
        video.srcObject = stream;
        video.play();
        error = null;
      } catch (err) {
        console.error("Error accessing camera:", err);
        error = "Could not access the camera. Please ensure you have granted permissions.";
      }
    }
  
    function switchCamera() {
      facingMode = facingMode === 'user' ? 'environment' : 'user';
      startCamera();
    }
  
    function capturePhoto() {
      if (!video || !canvas) return;
      
      const context = canvas.getContext('2d');
      canvas.width = video.videoWidth;
      canvas.height = video.videoHeight;
      context.drawImage(video, 0, 0, canvas.width, canvas.height);
      
      capturedImage = canvas.toDataURL('image/png');
    }
  
    function downloadPhoto() {
      if (!capturedImage) return;
      
      const a = document.createElement('a');
      a.href = capturedImage;
      a.download = `photo-${new Date().toISOString()}.png`;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
    }
  
    function retakePhoto() {
      capturedImage = null;
    }
  
    onMount(() => {
      startCamera();
    });
  
    onDestroy(() => {
      if (stream) {
        stream.getTracks().forEach(track => track.stop());
      }
    });
  </script>
  
  <div class="camera-app">
    {#if error}
      <div class="error-message">
        <p>{error}</p>
        <button on:click={startCamera}>Try Again</button>
      </div>
    {:else}
      {#if capturedImage}
        <div class="preview-container">
          <img src={capturedImage} alt="Captured" />
          <div class="controls">
            <button on:click={retakePhoto} class="btn secondary">Retake</button>
            <button on:click={downloadPhoto} class="btn primary">Save Photo</button>
          </div>
        </div>
      {:else}
        <div class="viewfinder">
          <!-- svelte-ignore a11y-media-has-caption -->
          <video bind:this={video} playsinline muted></video>
          <canvas bind:this={canvas} style="display: none;"></canvas>
          
          <div class="controls">
            <button on:click={switchCamera} class="btn icon-btn" title="Switch Camera">
              <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M20 5H9l-7 7 7 7h11a2 2 0 0 0 2-2V7a2 2 0 0 0-2-2Z"/><line x1="18" y1="9" x2="12" y2="15"/><line x1="12" y1="9" x2="18" y2="15"/></svg>
            </button>
            
            <button on:click={capturePhoto} class="capture-btn" aria-label="Take Photo"></button>
            
            <div class="spacer"></div> 
          </div>
        </div>
      {/if}
    {/if}
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
    }
  
    .viewfinder, .preview-container {
      position: relative;
      width: 100%;
      height: 100%;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      overflow: hidden;
    }
  
    video, img {
      width: 100%;
      height: 100%;
      object-fit: cover;
    }
  
    .controls {
      position: absolute;
      bottom: 0;
      left: 0;
      width: 100%;
      padding: 2rem;
      background: linear-gradient(transparent, rgba(0,0,0,0.8));
      display: flex;
      justify-content: space-around;
      align-items: center;
      padding-bottom: env(safe-area-inset-bottom, 20px);
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
  
    .btn:hover {
        background: rgba(255, 255, 255, 0.3);
    }
  
    .btn.primary {
      background: white;
      color: black;
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
      content: '';
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
