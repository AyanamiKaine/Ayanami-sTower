<script>
    import './style.css';
  import { onMount } from "svelte";
  import { state } from "./core/State.js";
  import { HistoryManager } from "./core/History.js";
  import { Renderer } from "./render/Renderer.js";
  import { InputManager } from "./input/InputManager.js";
  import {
    initializeMap,
    generateRandomProvinces,
    finishShape,
    deleteSelectedProvinces,
    clearCanvas,
    undoLastPoint,
  } from "./core/Operations.js";
  import { parseMapData, generateExportData } from "./utils/FileIO.js";
  import { colorToHex, getRandomColor } from "./utils/Colors.js";

  let canvas;
  let renderer;
  let history;
  let inputManager;

  // UI State
  let showStartupModal = true;
  let showBenchmarkModal = false;
  let showExportModal = false;
  let showTemplateModal = false;
  let showOwnerModal = false;

  let mapWidth = 1920;
  let mapHeight = 1080;
  let benchmarkCount = 1000;
  let exportJson = "";
  let templateName = "";
  
  // Reactive State from Core
  let appMode = "draw";
  let mapMode = "province";
  let isMapInitialized = false;
  let provinceCount = 0;
  let vertexCount = 0;
  let zoomLevel = 100;
  let mapResolution = "-";
  let visibleProvs = 0;
  let fps = 60;
  let statusText = "Ready";
  let statusClass = "text-yellow-400";
  
  let owners = [];
  let selectedProvincesCount = 0;
  let selectedProvince = null; // If only one selected
  let selectedRefImage = null;
  let isEditRefsMode = false;
  let showGrid = true;
  let showLOD = true;
  
  let draftPointsLength = 0;
  let historyIndex = -1;
  let historyStackLength = 0;

  // Bindings
  let importFileInput;
  let refImageInput;
  let exportTextarea;

  // Inspector
  let newOwnerName = "";
  let newOwnerColor = "#ff0000";
  let newMetaKey = "";
  let newMetaValue = "";
  let selectedTemplate = "";
  let templateLibrary = {};

  function updateUI() {
    // Pull data from global state
    appMode = state.appMode;
    mapMode = state.mapMode;
    isMapInitialized = state.isMapInitialized;
    owners = [...state.owners];
    selectedProvincesCount = state.selectedProvinces.size;
    selectedProvince = selectedProvincesCount === 1 ? state.selectedProvinces.values().next().value : null;
    selectedRefImage = state.selectedRefImage;
    isEditRefsMode = state.isEditRefsMode;
    templateLibrary = state.templateLibrary;
    
    provinceCount = state.provinces.length;
    vertexCount = state.provinces.reduce((acc, p) => acc + p.points.length, 0);
    zoomLevel = Math.round(state.camera.zoom * 100);
    mapResolution = `${state.mapConfig.width}x${state.mapConfig.height}`;
    
    draftPointsLength = state.draftPoints.length;
    historyIndex = history.historyIndex;
    historyStackLength = history.historyStack.length;

    updateStatusText();
  }

  function updateStatusText() {
    if (appMode === "draw") {
        statusText = draftPointsLength > 0 ? "Drawing..." : "Ready to Draw";
        statusClass = "text-yellow-400 font-bold col-span-2 mt-1";
    } else if (appMode === "select") {
        statusText = "Select Mode: Click/Drag to select.";
        statusClass = "text-blue-300 font-bold col-span-2 mt-1";
    } else {
        if (isEditRefsMode) {
            statusText = selectedRefImage ? "Image Selected" : "Select Image to Edit";
            statusClass = "text-purple-400 font-bold col-span-2 mt-1";
        } else if (state.isBoxSelecting) {
            statusText = "Box Selecting...";
            statusClass = "text-blue-300 font-bold col-span-2 mt-1";
        } else if (state.isDraggingVertex) {
            statusText = state.activeSnapTarget ? "Snapping to Vertex!" : "Moving Vertex";
        } else if (state.isDraggingProvince) {
            statusText = `Moving ${selectedProvincesCount} Province(s)`;
        } else if (state.hoveredSegment) {
            statusText = "Double-Click to Add Point";
        } else if (state.hoveredVertex) {
            statusText = "Double-Click to Delete Vertex";
        } else if (selectedProvincesCount > 0) {
            statusText = `${selectedProvincesCount} Province(s) Selected`;
        } else {
            statusText = "Select Province, Drag to Box Select";
        }
        
        if (!state.isDraggingVertex && !state.activeSnapTarget && !state.isBoxSelecting && !isEditRefsMode) {
             statusClass = "text-blue-400 font-bold col-span-2 mt-1";
        }
    }
  }

  onMount(() => {
    renderer = new Renderer(canvas);
    
    // Mock UI Manager for InputManager compatibility
    const uiManagerMock = {
        updateUI: () => {
            updateUI();
            // Force Svelte update
            owners = owners; 
        },
        setMode: (m) => setMode(m),
        syncColorPicker: (c) => { /* handled by reactivity */ },
        deleteSelectedRef: () => deleteRef()
    };

    history = new HistoryManager({
        onUpdate: () => {
            updateUI();
            renderer.render();
        }
    });

    inputManager = new InputManager(canvas, history, renderer, uiManagerMock);
    
    // Hook into renderer for FPS
    const originalDraw = renderer.draw.bind(renderer);
    renderer.draw = (time) => {
        originalDraw(time);
        // Update FPS occasionally? Or just bind it.
        // The renderer updates a DOM element directly. We should change that.
        // For now, let's leave it or poll it.
    };
    
    // Handle Resize
    window.addEventListener('resize', () => renderer.resize());
    renderer.resize();
    renderer.start();
  });

  // Actions
  function initMap() {
    initializeMap(mapWidth, mapHeight, history);
    const dpr = window.devicePixelRatio || 1;
    state.camera.x = (renderer.canvas.width / dpr - mapWidth) / 2;
    state.camera.y = (renderer.canvas.height / dpr - mapHeight) / 2;
    if (state.camera.x > 0) state.camera.x = 0;
    if (state.camera.y > 0) state.camera.y = 0;
    state.camera.zoom = 1;
    if (mapWidth > 2000) state.camera.zoom = 0.5;

    showStartupModal = false;
    setMode("draw");
    updateUI();
    renderer.render();
  }

  function setMode(mode) {
    if (!state.isMapInitialized) return;
    state.appMode = mode;
    state.selectedProvinces.clear();
    state.selectedRefImage = null;
    state.hoveredSegment = null;
    
    if (mode === "draw") {
        renderer.canvas.style.cursor = "crosshair";
        if (isEditRefsMode) {
            isEditRefsMode = false;
            state.isEditRefsMode = false;
        }
    } else if (mode === "edit") {
        renderer.canvas.style.cursor = "default";
        state.draftPoints = [];
    } else if (mode === "select") {
        renderer.canvas.style.cursor = "default";
        state.draftPoints = [];
    }
    updateUI();
    renderer.render();
  }

  function handleImport(e) {
    const file = e.target.files[0];
    if (!file) return;
    const reader = new FileReader();
    reader.onload = (evt) => {
        try {
            const data = parseMapData(evt.target.result);
            state.mapConfig = data.map;
            if (data.templates) state.templateLibrary = data.templates;
            if (data.owners) state.owners = data.owners;
            else state.owners = [{ id: "0", name: "Unclaimed", color: "#9ca3af" }];
            
            state.provinces = data.provinces.map((p) => {
                const newProv = {
                    color: p.color,
                    ownerId: p.ownerId || "0",
                    points: p.points,
                    metadata: p.metadata || {},
                    bbox: { minX: 0, maxX: 0, minY: 0, maxY: 0 },
                };
                state.updateProvinceBounds(newProv);
                return newProv;
            });
            
            state.draftPoints = [];
            state.selectedProvinces.clear();
            state.referenceImages = [];
            state.isMapInitialized = true;
            
            showStartupModal = false;
            
            // Reset Camera
            const dpr = window.devicePixelRatio || 1;
            state.camera.x = (renderer.canvas.width / dpr - state.mapConfig.width) / 2;
            state.camera.y = (renderer.canvas.height / dpr - state.mapConfig.height) / 2;
            state.camera.zoom = 1;
            if (state.mapConfig.width > 2000) state.camera.zoom = 0.5;
            
            history.historyStack = [];
            history.historyIndex = -1;
            history.saveHistory();
            
            updateUI();
            renderer.render();
        } catch (err) {
            console.error(err);
            alert("Error parsing file.");
        }
    };
    reader.readAsText(file);
    e.target.value = "";
  }

  function handleRefUpload(e) {
      const file = e.target.files[0];
      if (!file) return;
      const reader = new FileReader();
      reader.onload = (evt) => {
          const img = new Image();
          img.onload = () => {
              const dpr = window.devicePixelRatio || 1;
              const centerX = (renderer.canvas.width / dpr / 2 - state.camera.x) / state.camera.zoom;
              const centerY = (renderer.canvas.height / dpr / 2 - state.camera.y) / state.camera.zoom;
              
              state.referenceImages.push({
                  id: Date.now(),
                  img: img,
                  x: centerX - img.width / 2,
                  y: centerY - img.height / 2,
                  width: img.width,
                  height: img.height,
                  opacity: 0.5,
              });
              renderer.render();
              
              if (!isEditRefsMode) {
                  alert("Image Loaded. Enable 'Edit References' to move/resize it.");
                  isEditRefsMode = true;
                  state.isEditRefsMode = true;
                  updateUI();
              }
          };
          img.src = evt.target.result;
      };
      reader.readAsDataURL(file);
      e.target.value = "";
  }

  function runBenchmark() {
      generateRandomProvinces(benchmarkCount, state.mapConfig.width, state.mapConfig.height);
      showBenchmarkModal = false;
      setMode("edit");
      history.saveHistory();
      renderer.render();
      updateUI();
  }

  function openExport() {
      const data = generateExportData();
      exportJson = JSON.stringify(data, null, 2);
      showExportModal = true;
  }

  function copyExport() {
      navigator.clipboard.writeText(exportJson);
      alert("Copied to clipboard!");
  }

  function downloadExport() {
      const dataStr = "data:text/json;charset=utf-8," + encodeURIComponent(exportJson);
      const downloadAnchorNode = document.createElement("a");
      downloadAnchorNode.setAttribute("href", dataStr);
      downloadAnchorNode.setAttribute("download", "map_data.json");
      document.body.appendChild(downloadAnchorNode);
      downloadAnchorNode.click();
      downloadAnchorNode.remove();
  }

  function addOwner() {
      const newId = Date.now().toString();
      state.owners.push({
          id: newId,
          name: newOwnerName || `Empire ${state.owners.length}`,
          color: newOwnerColor
      });
      newOwnerName = "";
      history.saveHistory();
      updateUI();
  }

  function deleteOwner(id) {
      if (id === "0") return;
      if (confirm("Delete owner? Provinces will become Unclaimed.")) {
          state.provinces.forEach(p => {
              if (p.ownerId === id) p.ownerId = "0";
          });
          state.owners = state.owners.filter(o => o.id !== id);
          history.saveHistory();
          renderer.render();
          updateUI();
      }
  }

  function updateOwnerColor(owner, color) {
      owner.color = color;
      history.saveHistory();
      renderer.render();
  }

  function updateOwnerName(owner, name) {
      owner.name = name;
      history.saveHistory();
  }

  function setMapMode(mode) {
      state.mapMode = mode;
      mapMode = mode;
      renderer.render();
  }

  function toggleEditRefs() {
      isEditRefsMode = !isEditRefsMode;
      state.isEditRefsMode = isEditRefsMode;
      state.selectedRefImage = null;
      updateUI();
      renderer.render();
  }

  function deleteRef() {
      if (state.selectedRefImage) {
          state.referenceImages = state.referenceImages.filter(r => r !== state.selectedRefImage);
          state.selectedRefImage = null;
          updateUI();
          renderer.render();
      }
  }

  function saveTemplate() {
      if (templateName && selectedProvince) {
          state.templateLibrary[templateName] = JSON.parse(JSON.stringify(selectedProvince.metadata));
          templateName = "";
          showTemplateModal = false;
          updateUI();
      }
  }

  function applyTemplate() {
      if (selectedTemplate && state.templateLibrary[selectedTemplate]) {
          const tmpl = state.templateLibrary[selectedTemplate];
          state.selectedProvinces.forEach(p => {
              Object.assign(p.metadata, tmpl);
          });
          history.saveHistory();
          updateUI();
      }
  }

  function deleteTemplate() {
      if (selectedTemplate && confirm(`Delete template ${selectedTemplate}?`)) {
          delete state.templateLibrary[selectedTemplate];
          selectedTemplate = "";
          updateUI();
      }
  }

  function addMetadata() {
      if (newMetaKey && selectedProvince) {
          selectedProvince.metadata[newMetaKey] = newMetaValue;
          newMetaKey = "";
          newMetaValue = "";
          history.saveHistory();
          updateUI();
      }
  }

  function removeMetadata(key) {
      if (selectedProvince) {
          delete selectedProvince.metadata[key];
          history.saveHistory();
          updateUI();
      }
  }

  function updateProvinceColor(color) {
      if (selectedProvince) {
          selectedProvince.color = color;
          history.saveHistory();
          renderer.render();
      }
  }
  
  function randomizeColor() {
      if (selectedProvince) {
          selectedProvince.color = getRandomColor();
          history.saveHistory();
          renderer.render();
          updateUI();
      }
  }

  function handleFinish() {
      finishShape(history);
      updateUI();
      renderer.render();
  }

  function handleUndo() {
      if (appMode === "draw") {
          undoLastPoint();
          renderer.render();
          updateUI();
      } else {
          history.undo();
      }
  }

  function handleRedo() {
      history.redo();
  }

  function handleDelete() {
      deleteSelectedProvinces(history);
      updateUI();
      renderer.render();
  }

  function handleClear() {
      clearCanvas(history);
      updateUI();
      renderer.render();
  }
  
  function toggleGrid() {
      showGrid = !showGrid;
      // Renderer checks DOM element usually, but we should pass it or update renderer state
      // The renderer currently looks for document.getElementById("gridToggle")
      // We need to fix Renderer to accept state or we need to mock the element.
      // Ideally, we refactor Renderer to accept config.
      // For now, let's rely on the fact that we will render the checkbox with the same ID.
      setTimeout(() => renderer.render(), 0);
  }
  
  function toggleLOD() {
      showLOD = !showLOD;
      setTimeout(() => renderer.render(), 0);
  }

</script>

<main>
    <div class="planetmap-app">
    <!-- Hidden Inputs -->
    <input type="file" bind:this={importFileInput} accept=".json" style="display: none;" on:change={handleImport}>
    <input type="file" bind:this={refImageInput} accept="image/*" style="display: none;" on:change={handleRefUpload}>

    <!-- Startup Modal -->
    {#if showStartupModal}
    <div class="modal-overlay">
        <div class="modal-content" style="width: 400px;">
            <h2 class="text-2xl font-bold text-white mb-4">Create New Map</h2>
            <p class="text-gray-400 text-sm mb-6">Define the resolution or load an existing map.</p>

            <div class="flex gap-4">
                <div class="input-group flex-1">
                    <label>
                        Width (px)
                        <input type="number" bind:value={mapWidth}>
                    </label>
                </div>
                <div class="input-group flex-1">
                    <label>
                        Height (px)
                        <input type="number" bind:value={mapHeight}>
                    </label>
                </div>
            </div>

            <button class="w-full bg-blue-600 hover:bg-blue-500 text-white font-bold py-3 px-4 rounded transition mt-2" on:click={initMap}>
                Create Empty Map
            </button>

            <div class="text-center text-gray-500 text-xs my-3">- OR -</div>

            <button class="w-full bg-gray-700 hover:bg-gray-600 text-white font-bold py-3 px-4 rounded transition border border-gray-600" on:click={() => importFileInput.click()}>
                Import Existing Map (.json)
            </button>
        </div>
    </div>
    {/if}

    <!-- Benchmark Modal -->
    {#if showBenchmarkModal}
    <div class="modal-overlay">
        <div class="modal-content" style="width: 400px;">
            <h2 class="text-2xl font-bold text-white mb-2">Benchmark Tool</h2>
            <div class="input-group">
                <label>
                    Number of Provinces
                    <input type="number" bind:value={benchmarkCount} min="100" max="50000">
                </label>
            </div>
            <div class="flex gap-3 mt-4">
                <button class="flex-1 bg-gray-600 hover:bg-gray-500 text-white font-bold py-3 px-4 rounded transition" on:click={() => showBenchmarkModal = false}>Cancel</button>
                <button class="flex-1 bg-purple-600 hover:bg-purple-500 text-white font-bold py-3 px-4 rounded transition" on:click={runBenchmark}>Run Benchmark</button>
            </div>
        </div>
    </div>
    {/if}

    <!-- Template Modal -->
    {#if showTemplateModal}
    <div class="modal-overlay">
        <div class="modal-content" style="width: 400px;">
            <h2 class="text-xl font-bold text-white mb-4">Save Template</h2>
            <div class="input-group">
                <label>
                    Template Name
                    <input type="text" bind:value={templateName} placeholder="e.g. Dense Forest">
                </label>
            </div>
            <div class="flex gap-3 mt-4">
                <button class="flex-1 bg-gray-600 hover:bg-gray-500 text-white font-bold py-2 px-4 rounded transition" on:click={() => showTemplateModal = false}>Cancel</button>
                <button class="flex-1 bg-blue-600 hover:bg-blue-500 text-white font-bold py-2 px-4 rounded transition" on:click={saveTemplate}>Save</button>
            </div>
        </div>
    </div>
    {/if}

    <!-- Owner Modal -->
    {#if showOwnerModal}
    <div class="modal-overlay">
        <div class="modal-content" style="width: 500px;">
            <h2 class="text-xl font-bold text-white mb-4">Manage Owners</h2>
            <div class="flex gap-2 mb-4">
                <input type="text" bind:value={newOwnerName} placeholder="Empire Name" aria-label="New Owner Name" class="flex-1 bg-gray-800 border border-gray-600 rounded px-2 py-1 text-white outline-none focus:border-blue-500">
                <input type="color" bind:value={newOwnerColor} aria-label="New Owner Color" class="h-8 w-8 cursor-pointer bg-transparent" style="padding: 0; border: none;">
                <button class="bg-blue-600 hover:bg-blue-500 text-white font-bold px-4 rounded transition" on:click={addOwner}>Add</button>
            </div>
            <div class="max-h-64 overflow-y-auto mb-4 border border-gray-700 rounded p-2 bg-gray-900">
                {#each owners as owner (owner.id)}
                <div class="flex items-center gap-2 mb-2 p-2 bg-gray-700 rounded">
                    <input type="color" value={owner.color} aria-label="Owner Color" on:input={(e) => updateOwnerColor(owner, e.target.value)} class="w-8 h-8 rounded cursor-pointer border-none p-0">
                    <input type="text" value={owner.name} aria-label="Owner Name" on:change={(e) => updateOwnerName(owner, e.target.value)} class="flex-1 bg-gray-800 text-white px-2 py-1 rounded border border-gray-600">
                    <button class="text-red-400 hover:text-red-300 font-bold px-2" disabled={owner.id === "0"} on:click={() => deleteOwner(owner.id)} aria-label="Delete Owner">&times;</button>
                </div>
                {/each}
            </div>
            <button class="w-full bg-gray-600 hover:bg-gray-500 text-white font-bold py-2 rounded transition" on:click={() => showOwnerModal = false}>Close</button>
        </div>
    </div>
    {/if}

    <!-- Export Modal -->
    {#if showExportModal}
    <div class="modal-overlay">
        <div class="modal-content">
            <h2 class="text-2xl font-bold text-white mb-2">Export Map Data</h2>
            <textarea readonly value={exportJson}></textarea>
            <div class="flex gap-3">
                <button class="px-4 bg-gray-600 hover:bg-gray-500 text-white font-bold py-2 rounded transition" on:click={() => showExportModal = false}>Close</button>
                <div class="flex-1"></div>
                <button class="px-4 bg-blue-600 hover:bg-blue-500 text-white font-bold py-2 rounded transition" on:click={copyExport}>Copy to Clipboard</button>
                <button class="px-4 bg-green-600 hover:bg-green-500 text-white font-bold py-2 rounded transition" on:click={downloadExport}>Download .json</button>
            </div>
        </div>
    </div>
    {/if}

    <!-- Left UI Controls -->
    {#if isMapInitialized}
    <div class="ui-panel w-72">
        <h1 class="text-xl font-bold text-white">Province Maker</h1>

        <div class="mode-toggle">
            <button class="mode-btn {appMode === 'draw' ? 'active' : ''}" on:click={() => setMode('draw')}>DRAW</button>
            <button class="mode-btn {appMode === 'edit' ? 'active' : ''}" on:click={() => setMode('edit')}>EDIT</button>
            <button class="mode-btn {appMode === 'select' ? 'active' : ''}" on:click={() => setMode('select')}>SELECT</button>
        </div>

        <div class="mt-2 mb-2">
            <div class="text-xs text-gray-400 font-bold uppercase mb-1">Map Mode</div>
            <div class="flex bg-gray-700 rounded p-1 gap-1">
                <button class="flex-1 py-1 text-xs font-bold rounded transition {mapMode === 'province' ? 'bg-blue-600 text-white' : 'text-gray-300 hover:bg-gray-600'}" on:click={() => setMapMode('province')}>Province</button>
                <button class="flex-1 py-1 text-xs font-bold rounded transition {mapMode === 'owner' ? 'bg-blue-600 text-white' : 'text-gray-300 hover:bg-gray-600'}" on:click={() => setMapMode('owner')}>Political</button>
            </div>
        </div>

        <div class="text-sm text-gray-400 leading-relaxed">
            {#if appMode === 'draw'}
                Click to draw. Enter to finish.
            {:else if appMode === 'edit'}
                Drag to move vertices/provinces. Del to delete.
            {:else}
                Click/Drag to select. Cannot move items.
            {/if}
        </div>

        <div class="flex flex-col gap-2 pt-2 border-t border-gray-600">
            {#if appMode === 'draw'}
            <button class="bg-green-600 hover:bg-green-500 text-white font-bold py-2 px-4 rounded transition disabled:opacity-50" disabled={draftPointsLength < 3} on:click={handleFinish}>
                Finish Shape (Enter)
            </button>
            {/if}

            <div class="flex gap-2">
                <button class="flex-1 bg-gray-600 hover:bg-gray-500 text-white font-bold py-2 px-2 rounded transition disabled:opacity-50 text-sm" disabled={appMode === 'draw' ? draftPointsLength === 0 : historyIndex <= 0} on:click={handleUndo}>
                    {appMode === 'draw' ? 'Undo Point (Z)' : 'Undo (Ctrl+Z)'}
                </button>
                <button class="flex-1 bg-gray-600 hover:bg-gray-500 text-white font-bold py-2 px-2 rounded transition disabled:opacity-50 text-sm" disabled={historyIndex >= historyStackLength - 1} on:click={handleRedo}>
                    Redo (Ctrl+Y)
                </button>
            </div>

            {#if appMode !== 'draw'}
            <button class="bg-red-600 hover:bg-red-500 text-white font-bold py-2 px-4 rounded transition" disabled={selectedProvincesCount === 0} on:click={handleDelete}>
                Delete Selected (Del)
            </button>
            {/if}

            <button class="w-full bg-purple-900 hover:bg-purple-700 text-white font-bold py-2 px-4 rounded transition mt-2 text-sm border border-purple-700" on:click={() => showOwnerModal = true}>
                Manage Owners
            </button>

            <div class="grid grid-cols-2 gap-2 mt-2">
                <button class="bg-purple-900 hover:bg-purple-700 text-white font-bold py-2 px-2 text-sm rounded transition" on:click={() => showBenchmarkModal = true}>
                    Benchmark
                </button>
                <button class="bg-blue-900 hover:bg-blue-700 text-white font-bold py-2 px-2 text-sm rounded transition" on:click={openExport}>
                    Export JSON
                </button>
            </div>

            <button class="bg-blue-800 hover:bg-blue-700 text-white font-bold py-2 px-4 text-sm rounded transition w-full mt-2 border border-blue-600" on:click={() => importFileInput.click()}>
                Import Map (JSON)
            </button>

            <!-- Reference Section -->
            <div class="mt-2 pt-2 border-t border-gray-600">
                <div class="text-xs text-gray-400 font-bold uppercase mb-1">Refrence Images</div>
                <button class="w-full bg-gray-700 hover:bg-gray-600 text-white font-bold py-1 px-2 rounded text-sm transition mb-2" on:click={() => refImageInput.click()}>
                    Upload Reference
                </button>
                <div class="flex items-center gap-2">
                    <label class="flex items-center gap-2 cursor-pointer">
                        <input type="checkbox" checked={isEditRefsMode} on:change={toggleEditRefs} class="w-4 h-4 rounded bg-gray-700 border-gray-600 text-blue-600 focus:ring-blue-500">
                        <span class="text-sm text-gray-300 select-none">Edit References</span>
                    </label>
                </div>
            </div>

            <button class="bg-red-900 hover:bg-red-700 text-white font-bold py-2 px-4 rounded transition mt-2" on:click={handleClear}>
                Reset Canvas
            </button>

            <div class="flex items-center gap-2 mt-2 pt-2 border-t border-gray-600">
                <input type="checkbox" id="gridToggle" checked={showGrid} on:change={toggleGrid} class="w-4 h-4 rounded bg-gray-700 border-gray-600 text-blue-600 focus:ring-blue-500">
                <label for="gridToggle" class="text-sm text-gray-300 select-none cursor-pointer">Show Grid</label>
            </div>

            <div class="flex items-center gap-2 mt-2 pt-2 border-t border-gray-600">
                <input type="checkbox" id="lodToggle" checked={showLOD} on:change={toggleLOD} class="w-4 h-4 rounded bg-gray-700 border-gray-600 text-blue-600 focus:ring-blue-500">
                <label for="lodToggle" class="text-sm text-gray-300 select-none cursor-pointer">Adaptive Detail (LOD)</label>
            </div>
        </div>
    </div>
    {/if}

    <!-- Inspector Panel -->
    {#if (isEditRefsMode && selectedRefImage) || (!isEditRefsMode && appMode !== 'draw' && selectedProvince)}
    <div class="inspector-panel visible">
        <h2 class="text-lg font-bold text-white mb-2 border-b border-gray-600 pb-2">
            {isEditRefsMode ? 'Reference Image' : 'Province Properties'}
        </h2>

        {#if isEditRefsMode && selectedRefImage}
        <div>
            <label class="block mb-4">
                <span class="text-sm text-gray-300 mb-2 block">Opacity</span>
                <input type="range" min="0" max="100" value={selectedRefImage.opacity * 100} on:input={(e) => { selectedRefImage.opacity = e.target.value / 100; renderer.render(); }} class="w-full">
            </label>
            <button class="w-full bg-red-600 hover:bg-red-500 text-white font-bold py-2 px-4 rounded text-sm transition" on:click={deleteRef}>
                Remove Image
            </button>
        </div>
        {:else if selectedProvince}
        <div>
            <!-- Owner Selection -->
            <div class="mb-4 border-b border-gray-600 pb-4">
                <label class="block">
                    <span class="text-xs text-gray-400 mb-2 uppercase font-bold block">Owner</span>
                    <select value={selectedProvince.ownerId} on:change={(e) => { selectedProvince.ownerId = e.target.value; history.saveHistory(); renderer.render(); }} class="w-full bg-gray-800 border border-gray-600 rounded px-2 py-1 text-sm text-white focus:border-blue-500 outline-none">
                        {#each owners as owner}
                        <option value={owner.id}>{owner.name}</option>
                        {/each}
                    </select>
                </label>
            </div>

            <!-- Appearance -->
            <div class="mb-4 border-b border-gray-600 pb-4">
                <div class="text-xs text-gray-400 mb-2 uppercase font-bold">Appearance</div>
                <div class="flex gap-2 items-center">
                    <label class="flex items-center gap-2 flex-1 cursor-pointer">
                        <input type="color" value={colorToHex(null, selectedProvince.color)} on:input={(e) => updateProvinceColor(e.target.value)} title="Change Color">
                        <span class="text-sm text-gray-300">Province Color</span>
                    </label>
                    <button class="text-xs bg-gray-700 hover:bg-gray-600 text-white px-2 py-1 rounded transition" on:click={randomizeColor}>
                        Randomize
                    </button>
                </div>
            </div>

            <!-- Templates -->
            <div class="mb-4 border-b border-gray-600 pb-4">
                <div class="text-xs text-gray-400 mb-2 uppercase font-bold">Templates</div>
                <div class="flex gap-2 mb-2">
                    <select bind:value={selectedTemplate} aria-label="Select Template" class="flex-1 bg-gray-800 border border-gray-600 rounded px-2 py-1 text-sm text-white focus:border-blue-500 outline-none">
                        <option value="">Select Template...</option>
                        {#each Object.keys(templateLibrary) as name}
                        <option value={name}>{name}</option>
                        {/each}
                    </select>
                    <button class="bg-red-900 hover:bg-red-700 text-red-200 font-bold px-2 rounded text-sm" on:click={deleteTemplate} aria-label="Delete Template">&times;</button>
                </div>
                <div class="flex gap-2">
                    <button class="flex-1 bg-green-600 hover:bg-green-500 text-white font-bold py-1 px-2 rounded text-sm transition" on:click={applyTemplate}>Apply</button>
                    <button class="flex-1 bg-gray-700 hover:bg-gray-600 text-white font-bold py-1 px-2 rounded text-sm transition border border-gray-600" on:click={() => showTemplateModal = true}>Save New</button>
                </div>
            </div>

            <!-- Metadata -->
            <div class="mb-4">
                {#each Object.entries(selectedProvince.metadata) as [key, val]}
                <div class="meta-row">
                    <div class="meta-key">{key}</div>
                    <input type="text" value={val} aria-label={`Value for ${key}`} on:input={(e) => { selectedProvince.metadata[key] = e.target.value; history.saveHistory(); }} class="meta-val-input">
                    <button class="meta-del bg-transparent border-none" on:click={() => removeMetadata(key)} aria-label={`Remove ${key}`}>&times;</button>
                </div>
                {/each}
            </div>

            <div class="pt-2 border-t border-gray-600">
                <div class="text-xs text-gray-400 mb-2 uppercase font-bold">Add Metadata</div>
                <div class="flex gap-2 mb-2">
                    <input type="text" bind:value={newMetaKey} placeholder="Key" aria-label="New Metadata Key" class="flex-1 bg-gray-800 border border-gray-600 rounded px-2 py-1 text-sm text-white focus:border-blue-500 outline-none">
                    <input type="text" bind:value={newMetaValue} placeholder="Value" aria-label="New Metadata Value" class="flex-1 bg-gray-800 border border-gray-600 rounded px-2 py-1 text-sm text-white focus:border-blue-500 outline-none">
                </div>
                <button class="w-full bg-blue-600 hover:bg-blue-500 text-white font-bold py-1 px-2 rounded text-sm transition" on:click={addMetadata}>Add Property</button>
            </div>
        </div>
        {/if}
    </div>
    {/if}

    <!-- Stats -->
    {#if isMapInitialized}
    <div class="stats-panel">
        <div>Provinces: {provinceCount}</div>
        <div id="fpsDisplay" class="text-green-400 font-bold">FPS: {fps}</div>
        <div>Total Vertices: {vertexCount}</div>
        <div>Visible: {visibleProvs}</div>
        <div>Res: {mapResolution}</div>
        <div>Zoom: {zoomLevel}%</div>
        <div class={statusClass}>{statusText}</div>
    </div>
    {/if}

        <canvas bind:this={canvas} tabindex="0"></canvas>
    </div>
</main>

<style>
    /* Import global styles if needed, or paste them here */
    /* For now, we rely on global styles being imported in main.js or app.css */
</style>
