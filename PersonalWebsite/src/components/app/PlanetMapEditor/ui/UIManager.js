import { state } from "../core/State.js";
import {
	initializeMap,
	generateRandomProvinces,
	finishShape,
	deleteSelectedProvinces,
	clearCanvas,
	undoLastPoint,
} from "../core/Operations.js";
import { parseMapData, generateExportData } from "../utils/FileIO.js";
import { colorToHex, getRandomColor } from "../utils/Colors.js";

export class UIManager {
	constructor(history, renderer) {
		this.history = history;
		this.renderer = renderer;

		this.cacheDOMElements();
		this.setupEventListeners();
	}

	cacheDOMElements() {
		// File Input
		this.importFileInput = document.getElementById("importFileInput");
		this.refImageInput = document.getElementById("refImageInput");

		// Modal Elements
		this.startupModal = document.getElementById("startupModal");
		this.benchmarkModal = document.getElementById("benchmarkModal");
		this.exportModal = document.getElementById("exportModal");
		this.templateModal = document.getElementById("templateModal");

		this.createMapBtn = document.getElementById("createMapBtn");
		this.startupImportBtn = document.getElementById("startupImportBtn");
		this.mapWidthInput = document.getElementById("mapWidthInput");
		this.mapHeightInput = document.getElementById("mapHeightInput");

		this.openBenchmarkBtn = document.getElementById("openBenchmarkBtn");
		this.runBenchmarkBtn = document.getElementById("runBenchmarkBtn");
		this.cancelBenchmarkBtn = document.getElementById("cancelBenchmarkBtn");
		this.benchmarkCountInput = document.getElementById("benchmarkCountInput");

		this.openExportBtn = document.getElementById("openExportBtn");
		this.closeExportBtn = document.getElementById("closeExportBtn");
		this.copyExportBtn = document.getElementById("copyExportBtn");
		this.downloadExportBtn = document.getElementById("downloadExportBtn");
		this.exportTextarea = document.getElementById("exportTextarea");

		// Template Modal Elements
		this.templateNameInput = document.getElementById("templateNameInput");
		this.cancelTemplateBtn = document.getElementById("cancelTemplateBtn");
		this.confirmTemplateBtn = document.getElementById("confirmTemplateBtn");

		this.importBtn = document.getElementById("importBtn");

		this.mainUI = document.getElementById("mainUI");
		this.inspectorPanel = document.getElementById("inspectorPanel");
		this.inspectorTitle = document.getElementById("inspectorTitle");
		this.statsUI = document.getElementById("statsUI");

		// Inspector Elements (Metadata)
		this.provinceControls = document.getElementById("provinceControls");
		this.metaList = document.getElementById("metaList");
		this.newMetaKey = document.getElementById("newMetaKey");
		this.newMetaValue = document.getElementById("newMetaValue");
		this.addMetaBtn = document.getElementById("addMetaBtn");
		this.templateSelect = document.getElementById("templateSelect");
		this.applyTemplateBtn = document.getElementById("applyTemplateBtn");
		this.saveAsTemplateBtn = document.getElementById("saveAsTemplateBtn");
		this.deleteTemplateBtn = document.getElementById("deleteTemplateBtn");

		// Color Elements
		this.provColorPicker = document.getElementById("provColorPicker");
		this.randomColorBtn = document.getElementById("randomColorBtn");

		// Owner Elements
		this.openOwnerManagerBtn = document.getElementById("openOwnerManagerBtn");
		this.ownerModal = document.getElementById("ownerModal");
		this.closeOwnerModalBtn = document.getElementById("closeOwnerModalBtn");
		this.ownerList = document.getElementById("ownerList");
		this.addNewOwnerBtn = document.getElementById("addNewOwnerBtn");
		this.provOwnerSelect = document.getElementById("provOwnerSelect");

		// Map Mode Elements
		this.modeProvBtn = document.getElementById("modeProvBtn");
		this.modeOwnerBtn = document.getElementById("modeOwnerBtn");

		// Inspector Elements (References)
		this.refImageControls = document.getElementById("refImageControls");
		this.refOpacitySlider = document.getElementById("refOpacitySlider");
		this.deleteRefBtn = document.getElementById("deleteRefBtn");
		this.uploadRefBtn = document.getElementById("uploadRefBtn");
		this.editRefsToggle = document.getElementById("editRefsToggle");

		this.finishBtn = document.getElementById("finishBtn");
		this.undoBtn = document.getElementById("undoBtn");
		this.redoBtn = document.getElementById("redoBtn");
		this.deleteProvBtn = document.getElementById("deleteProvBtn");
		this.clearBtn = document.getElementById("clearBtn");
		this.gridToggle = document.getElementById("gridToggle");
		this.lodToggle = document.getElementById("lodToggle");
		this.statusText = document.getElementById("statusText");

		this.provinceCountDisplay = document.getElementById("provinceCount");
		this.vertexCountDisplay = document.getElementById("vertexCount");
		this.zoomLevelDisplay = document.getElementById("zoomLevel");
		this.mapResolutionDisplay = document.getElementById("mapResolution");
		this.visibleProvsDisplay = document.getElementById("visibleProvs");

		this.drawModeBtn = document.getElementById("drawModeBtn");
		this.editModeBtn = document.getElementById("editModeBtn");
		this.selectModeBtn = document.getElementById("selectModeBtn");
		this.instructions = document.getElementById("instructions");
	}

	setupEventListeners() {
		// --- Initialization ---
		this.createMapBtn.addEventListener("click", () => {
			const w = parseInt(this.mapWidthInput.value) || 1920;
			const h = parseInt(this.mapHeightInput.value) || 1080;
			this.initMap(w, h);
		});

		// --- Import Logic ---
		this.importBtn.addEventListener("click", () =>
			this.importFileInput.click()
		);
		this.startupImportBtn.addEventListener("click", () =>
			this.importFileInput.click()
		);

		this.importFileInput.addEventListener("change", (e) => {
			const file = e.target.files[0];
			if (!file) return;

			const reader = new FileReader();
			reader.onload = (e) => {
				try {
					const data = parseMapData(e.target.result);

					state.mapConfig = data.map;
					if (data.templates) {
						state.templateLibrary = data.templates;
						this.updateTemplateDropdown();
					}

					if (data.owners) {
						state.owners = data.owners;
					} else {
						state.owners = [{ id: "0", name: "Unclaimed", color: "#9ca3af" }];
					}

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
					this.startupModal.style.display = "none";
					this.mainUI.classList.remove("hidden");
					this.statsUI.classList.remove("hidden");

					const dpr = window.devicePixelRatio || 1;
					state.camera.x =
						(this.renderer.canvas.width / dpr - state.mapConfig.width) / 2;
					state.camera.y =
						(this.renderer.canvas.height / dpr - state.mapConfig.height) / 2;
					state.camera.zoom = 1;
					if (state.mapConfig.width > 2000) state.camera.zoom = 0.5;

					this.mapResolutionDisplay.textContent = `Res: ${state.mapConfig.width}x${state.mapConfig.height}`;

					// Reset History on Import
					this.history.historyStack = [];
					this.history.historyIndex = -1;
					this.history.saveHistory();

					this.updateOwnerDropdown();
					this.updateUI();
					this.renderer.render();
				} catch (err) {
					console.error(err);
					alert("Error parsing file.");
				}
			};
			reader.readAsText(file);
			this.importFileInput.value = "";
		});

		// --- Benchmark ---
		this.openBenchmarkBtn.addEventListener("click", () =>
			this.benchmarkModal.classList.remove("hidden-modal")
		);
		this.cancelBenchmarkBtn.addEventListener("click", () =>
			this.benchmarkModal.classList.add("hidden-modal")
		);

		this.runBenchmarkBtn.addEventListener("click", () => {
			const count = parseInt(this.benchmarkCountInput.value) || 1000;
			generateRandomProvinces(
				count,
				state.mapConfig.width,
				state.mapConfig.height
			);
			this.benchmarkModal.classList.add("hidden-modal");
			this.setMode("edit");
			this.history.saveHistory(); // Save state after benchmark
			this.renderer.render();
		});

		// --- Export Logic ---
		this.openExportBtn.addEventListener("click", () => {
			const exportData = generateExportData();
			this.exportTextarea.value = JSON.stringify(exportData, null, 2);
			this.exportModal.classList.remove("hidden-modal");
		});

		this.closeExportBtn.addEventListener("click", () => {
			this.exportModal.classList.add("hidden-modal");
		});

		this.copyExportBtn.addEventListener("click", () => {
			this.exportTextarea.select();
			document.execCommand("copy");
			const origText = this.copyExportBtn.textContent;
			this.copyExportBtn.textContent = "Copied!";
			setTimeout(() => (this.copyExportBtn.textContent = origText), 2000);
		});

		this.downloadExportBtn.addEventListener("click", () => {
			const dataStr =
				"data:text/json;charset=utf-8," +
				encodeURIComponent(this.exportTextarea.value);
			const downloadAnchorNode = document.createElement("a");
			downloadAnchorNode.setAttribute("href", dataStr);
			downloadAnchorNode.setAttribute("download", "map_data.json");
			document.body.appendChild(downloadAnchorNode);
			downloadAnchorNode.click();
			downloadAnchorNode.remove();
		});

		// --- Owner & Map Mode Logic ---
		this.openOwnerManagerBtn.addEventListener("click", () => {
			this.renderOwnerList();
			this.ownerModal.classList.remove("hidden-modal");
		});

		this.closeOwnerModalBtn.addEventListener("click", () => {
			this.ownerModal.classList.add("hidden-modal");
			this.updateOwnerDropdown();
			this.updateUI();
			this.renderer.render();
		});

		this.addNewOwnerBtn.addEventListener("click", () => {
			const newId = Date.now().toString();
			state.owners.push({
				id: newId,
				name: `Empire ${state.owners.length}`,
				color: getRandomColor(),
			});
			this.renderOwnerList();
			this.history.saveHistory();
		});

		this.provOwnerSelect.addEventListener("change", (e) => {
			const newOwnerId = e.target.value;
			state.selectedProvinces.forEach((p) => {
				p.ownerId = newOwnerId;
			});
			this.history.saveHistory();
			this.renderer.render();
		});

		this.modeProvBtn.addEventListener("click", () => {
			state.mapMode = "province";
			this.modeProvBtn.classList.add("bg-blue-600");
			this.modeProvBtn.classList.remove("bg-gray-600");
			this.modeOwnerBtn.classList.add("bg-gray-600");
			this.modeOwnerBtn.classList.remove("bg-blue-600");
			this.renderer.render();
		});

		this.modeOwnerBtn.addEventListener("click", () => {
			state.mapMode = "owner";
			this.modeOwnerBtn.classList.add("bg-blue-600");
			this.modeOwnerBtn.classList.remove("bg-gray-600");
			this.modeProvBtn.classList.add("bg-gray-600");
			this.modeProvBtn.classList.remove("bg-blue-600");
			this.renderer.render();
		});

		// --- Reference Image Logic ---
		this.uploadRefBtn.addEventListener("click", () => {
			this.refImageInput.click();
		});

		this.refImageInput.addEventListener("change", (e) => {
			const file = e.target.files[0];
			if (!file) return;

			const reader = new FileReader();
			reader.onload = (evt) => {
				const img = new Image();
				img.onload = () => {
					const dpr = window.devicePixelRatio || 1;
					const centerX =
						(this.renderer.canvas.width / dpr / 2 - state.camera.x) /
						state.camera.zoom;
					const centerY =
						(this.renderer.canvas.height / dpr / 2 - state.camera.y) /
						state.camera.zoom;

					state.referenceImages.push({
						id: Date.now(),
						img: img,
						x: centerX - img.width / 2,
						y: centerY - img.height / 2,
						width: img.width,
						height: img.height,
						opacity: 0.5,
					});
					this.renderer.render();
					this.refImageInput.value = "";

					if (!this.editRefsToggle.checked) {
						alert("Image Loaded. Enable 'Edit References' to move/resize it.");
						this.editRefsToggle.checked = true;
						state.isEditRefsMode = true;
						this.updateUI();
					}
				};
				img.src = evt.target.result;
			};
			reader.readAsDataURL(file);
		});

		this.editRefsToggle.addEventListener("change", () => {
			state.selectedRefImage = null;
			state.isEditRefsMode = this.editRefsToggle.checked;
			this.updateUI();
			this.renderer.render();
		});

		this.refOpacitySlider.addEventListener("input", (e) => {
			if (state.selectedRefImage) {
				state.selectedRefImage.opacity = e.target.value / 100;
				this.renderer.render();
			}
		});

		this.deleteRefBtn.addEventListener("click", () => {
			this.deleteSelectedRef();
		});

		// --- Template Modal Handlers ---
		this.saveAsTemplateBtn.addEventListener("click", () => {
			if (state.selectedProvinces.size !== 1) return;
			this.templateNameInput.value = "";
			this.templateModal.classList.remove("hidden-modal");
			this.templateNameInput.focus();
		});

		this.cancelTemplateBtn.addEventListener("click", () => {
			this.templateModal.classList.add("hidden-modal");
		});

		this.confirmTemplateBtn.addEventListener("click", () => {
			const name = this.templateNameInput.value.trim();
			if (name && state.selectedProvinces.size === 1) {
				const prov = state.selectedProvinces.values().next().value;
				state.templateLibrary[name] = JSON.parse(JSON.stringify(prov.metadata));
				this.updateTemplateDropdown();
				this.templateSelect.value = name;
				this.templateModal.classList.add("hidden-modal");
			}
		});

		this.deleteTemplateBtn.addEventListener("click", () => {
			const tmplName = this.templateSelect.value;
			if (tmplName && state.templateLibrary[tmplName]) {
				if (confirm(`Delete template "${tmplName}"?`)) {
					delete state.templateLibrary[tmplName];
					this.updateTemplateDropdown();
				}
			}
		});

		this.applyTemplateBtn.addEventListener("click", () => {
			const tmplName = this.templateSelect.value;
			if (!tmplName || !state.templateLibrary[tmplName]) return;

			const templateData = state.templateLibrary[tmplName];

			state.selectedProvinces.forEach((prov) => {
				for (const [key, val] of Object.entries(templateData)) {
					prov.metadata[key] = val;
				}
			});

			this.history.saveHistory(); // Save state

			if (state.selectedProvinces.size === 1) {
				this.renderMetadataUI(state.selectedProvinces.values().next().value);
			}
		});

		this.addMetaBtn.addEventListener("click", () => {
			const key = this.newMetaKey.value.trim();
			const val = this.newMetaValue.value.trim();

			if (key && state.selectedProvinces.size === 1) {
				const prov = state.selectedProvinces.values().next().value;
				prov.metadata[key] = val;
				this.history.saveHistory(); // Save state
				this.renderMetadataUI(prov);
				this.newMetaKey.value = "";
				this.newMetaValue.value = "";
			}
		});

		// --- Color Picker Logic ---
		this.provinceColorPickTarget = null;

		this.provColorPicker.addEventListener("click", () => {
			if (state.selectedProvinces.size === 1) {
				this.provinceColorPickTarget = state.selectedProvinces
					.values()
					.next().value;
			}
		});

		this.provColorPicker.addEventListener("change", (e) => {
			if (this.provinceColorPickTarget) {
				this.provinceColorPickTarget.color = e.target.value;
				this.history.saveHistory(); // Save state on finalized color pick
				this.renderer.render();

				// Restore selection if it changed (e.g. due to eyedropper click)
				state.selectedProvinces.clear();
				state.selectedProvinces.add(this.provinceColorPickTarget);
				this.updateUI();

				this.provinceColorPickTarget = null;
			} else if (state.selectedProvinces.size === 1) {
				const prov = state.selectedProvinces.values().next().value;
				prov.color = e.target.value;
				this.history.saveHistory(); // Save state on finalized color pick
				this.renderer.render();
			}
		});

		this.randomColorBtn.addEventListener("click", () => {
			if (state.selectedProvinces.size === 1) {
				const prov = state.selectedProvinces.values().next().value;
				prov.color = getRandomColor();
				// Sync picker
				this.provColorPicker.value = colorToHex(null, prov.color);
				this.history.saveHistory(); // Save state
				this.renderer.render();
			}
		});

		// --- Mode Switching ---
		this.drawModeBtn.addEventListener("click", () => this.setMode("draw"));
		this.editModeBtn.addEventListener("click", () => this.setMode("edit"));
		this.selectModeBtn.addEventListener("click", () => this.setMode("select"));
		this.deleteProvBtn.addEventListener("click", () => {
			deleteSelectedProvinces(this.history);
			this.updateUI();
			this.renderer.render();
		});

		// --- Operations ---
		this.finishBtn.addEventListener("click", () => {
			finishShape(this.history);
			this.updateUI();
			this.renderer.render();
		});
		this.undoBtn.addEventListener("click", () => {
			if (state.appMode === "draw") {
				undoLastPoint();
				this.renderer.render();
				this.updateUI();
			} else {
				this.history.undo();
			}
		});
		this.redoBtn.addEventListener("click", () => this.history.redo());
		this.clearBtn.addEventListener("click", () => {
			clearCanvas(this.history);
			this.updateUI();
			this.renderer.render();
		});
		this.gridToggle.addEventListener("change", () => this.renderer.render());
		this.lodToggle.addEventListener("change", () => this.renderer.render());

		// UX IMPROVEMENT: Pressing Enter in the inputs adds the metadata
		const handleInputEnter = (e) => {
			if (e.key === "Enter") this.addMetaBtn.click();
		};
		this.newMetaKey.addEventListener("keydown", handleInputEnter);
		this.newMetaValue.addEventListener("keydown", handleInputEnter);

		// New Template Save Enter Key
		this.templateNameInput.addEventListener("keydown", (e) => {
			if (e.key === "Enter") this.confirmTemplateBtn.click();
		});
	}

	initMap(w, h) {
		initializeMap(w, h, this.history);

		const dpr = window.devicePixelRatio || 1;
		state.camera.x = (this.renderer.canvas.width / dpr - w) / 2;
		state.camera.y = (this.renderer.canvas.height / dpr - h) / 2;
		if (state.camera.x > 0) state.camera.x = 0;
		if (state.camera.y > 0) state.camera.y = 0;
		state.camera.zoom = 1;
		if (w > 2000) state.camera.zoom = 0.5;

		this.startupModal.style.display = "none";
		this.mainUI.classList.remove("hidden");
		this.statsUI.classList.remove("hidden");
		this.mapResolutionDisplay.textContent = `Res: ${w}x${h}`;
		this.updateTemplateDropdown();
		this.setMode("draw");

		this.renderer.render();
	}

	setMode(mode) {
		if (!state.isMapInitialized) return;
		state.appMode = mode;
		state.selectedProvinces.clear();
		state.selectedRefImage = null;
		state.hoveredSegment = null;

		if (mode === "draw") {
			this.drawModeBtn.classList.add("active");
			this.editModeBtn.classList.remove("active");
			this.selectModeBtn.classList.remove("active");
			this.renderer.canvas.style.cursor = "crosshair";
			this.finishBtn.style.display = "block";
			this.deleteProvBtn.classList.add("hidden");

			// If editing refs, turn it off for draw mode safety
			if (this.editRefsToggle.checked) {
				this.editRefsToggle.checked = false;
				state.isEditRefsMode = false;
			}

			this.instructions.innerHTML = `Click to draw. Enter to finish.`;
		} else if (mode === "edit") {
			this.drawModeBtn.classList.remove("active");
			this.editModeBtn.classList.add("active");
			this.selectModeBtn.classList.remove("active");
			this.renderer.canvas.style.cursor = "default";
			this.finishBtn.style.display = "none";
			this.deleteProvBtn.classList.remove("hidden");
			state.draftPoints = [];
			this.instructions.innerHTML = `Drag to move vertices/provinces. Del to delete.`;
		} else if (mode === "select") {
			this.drawModeBtn.classList.remove("active");
			this.editModeBtn.classList.remove("active");
			this.selectModeBtn.classList.add("active");
			this.renderer.canvas.style.cursor = "default";
			this.finishBtn.style.display = "none";
			this.deleteProvBtn.classList.remove("hidden");
			state.draftPoints = [];
			this.instructions.innerHTML = `Click/Drag to select. Cannot move items.`;
		}
		this.updateUI();
		this.renderer.render();
	}

	updateUI() {
		const totalVerts = state.provinces.reduce(
			(acc, p) => acc + p.points.length,
			0
		);
		this.provinceCountDisplay.textContent = `Provinces: ${state.provinces.length}`;
		this.vertexCountDisplay.textContent = `Total Vertices: ${totalVerts}`;
		this.zoomLevelDisplay.textContent = `Zoom: ${Math.round(
			state.camera.zoom * 100
		)}%`;

		this.finishBtn.disabled = state.draftPoints.length < 3;

		// Undo Logic for UI
		if (state.appMode === "draw") {
			this.undoBtn.disabled = state.draftPoints.length === 0;
			this.undoBtn.textContent = "Undo Point (Z)";
		} else {
			this.undoBtn.disabled = this.history.historyIndex <= 0;
			this.redoBtn.disabled =
				this.history.historyIndex >= this.history.historyStack.length - 1;
			this.undoBtn.textContent = "Undo (Ctrl+Z)";
		}

		// Inspector Logic
		if (this.editRefsToggle.checked && state.selectedRefImage) {
			this.inspectorPanel.classList.add("visible");
			this.inspectorTitle.textContent = "Reference Image";
			this.provinceControls.classList.add("hidden");
			this.refImageControls.classList.remove("hidden");
			this.refOpacitySlider.value = state.selectedRefImage.opacity * 100;
		} else if (
			!this.editRefsToggle.checked &&
			state.appMode === "edit" &&
			state.selectedProvinces.size === 1
		) {
			this.inspectorPanel.classList.add("visible");
			this.inspectorTitle.textContent = "Province Properties";
			this.provinceControls.classList.remove("hidden");
			this.refImageControls.classList.add("hidden");
			const prov = state.selectedProvinces.values().next().value;
			this.renderMetadataUI(prov);
		} else {
			this.inspectorPanel.classList.remove("visible");
		}

		if (state.appMode === "draw") {
			this.statusText.textContent =
				state.draftPoints.length > 0 ? "Drawing..." : "Ready to Draw";
			this.statusText.className = "text-yellow-400 font-bold col-span-2 mt-1";
			this.deleteProvBtn.disabled = true;
		} else {
			if (this.editRefsToggle.checked) {
				this.statusText.textContent = state.selectedRefImage
					? "Image Selected"
					: "Select Image to Edit";
				this.statusText.className = "text-purple-400 font-bold col-span-2 mt-1";
			} else if (state.isBoxSelecting) {
				this.statusText.textContent = "Box Selecting...";
				this.statusText.className = "text-blue-300 font-bold col-span-2 mt-1";
			} else if (state.isDraggingVertex) {
				this.statusText.textContent = state.activeSnapTarget
					? "Snapping to Vertex!"
					: "Moving Vertex";
			} else if (state.isDraggingProvince)
				this.statusText.textContent = `Moving ${state.selectedProvinces.size} Province(s)`;
			else if (state.hoveredSegment)
				this.statusText.textContent = "Double-Click to Add Point";
			else if (state.hoveredVertex)
				this.statusText.textContent = "Double-Click to Delete Vertex";
			else if (state.selectedProvinces.size > 0)
				this.statusText.textContent = `${state.selectedProvinces.size} Province(s) Selected`;
			else this.statusText.textContent = "Select Province, Drag to Box Select";

			if (
				!state.isDraggingVertex &&
				!state.activeSnapTarget &&
				!state.isBoxSelecting &&
				!this.editRefsToggle.checked
			) {
				this.statusText.className = "text-blue-400 font-bold col-span-2 mt-1";
			}
			this.deleteProvBtn.disabled = state.selectedProvinces.size === 0;
			this.deleteProvBtn.style.opacity =
				state.selectedProvinces.size > 0 ? 1 : 0.5;
			this.deleteProvBtn.textContent =
				state.selectedProvinces.size > 1
					? `Delete Selected (${state.selectedProvinces.size})`
					: "Delete Selected (Del)";
		}
	}

	renderOwnerList() {
		this.ownerList.innerHTML = "";
		state.owners.forEach((owner) => {
			const row = document.createElement("div");
			row.className = "flex items-center gap-2 mb-2 p-2 bg-gray-700 rounded";

			const colorInput = document.createElement("input");
			colorInput.type = "color";
			colorInput.value = colorToHex(null, owner.color);
			colorInput.className = "w-8 h-8 rounded cursor-pointer border-none p-0";
			colorInput.addEventListener("change", (e) => {
				owner.color = e.target.value;
				this.history.saveHistory();
				this.renderer.render();
			});

			const nameInput = document.createElement("input");
			nameInput.type = "text";
			nameInput.value = owner.name;
			nameInput.className =
				"flex-1 bg-gray-800 text-white px-2 py-1 rounded border border-gray-600";
			nameInput.addEventListener("change", (e) => {
				owner.name = e.target.value;
				this.history.saveHistory();
				this.updateOwnerDropdown();
			});

			const delBtn = document.createElement("button");
			delBtn.innerHTML = "&times;";
			delBtn.className = "text-red-400 hover:text-red-300 font-bold px-2";
			if (owner.id === "0") {
				delBtn.disabled = true;
				delBtn.className = "text-gray-500 px-2 cursor-not-allowed";
				delBtn.title = "Cannot delete default owner";
			} else {
				delBtn.onclick = () => {
					if (
						confirm(
							`Delete owner "${owner.name}"? Provinces will become Unclaimed.`
						)
					) {
						state.provinces.forEach((p) => {
							if (p.ownerId === owner.id) p.ownerId = "0";
						});
						state.owners = state.owners.filter((o) => o.id !== owner.id);
						this.renderOwnerList();
						this.history.saveHistory();
						this.renderer.render();
					}
				};
			}

			row.appendChild(colorInput);
			row.appendChild(nameInput);
			row.appendChild(delBtn);
			this.ownerList.appendChild(row);
		});
	}

	updateOwnerDropdown() {
		this.provOwnerSelect.innerHTML = "";
		state.owners.forEach((owner) => {
			const opt = document.createElement("option");
			opt.value = owner.id;
			opt.textContent = owner.name;
			this.provOwnerSelect.appendChild(opt);
		});
	}

	updateTemplateDropdown() {
		this.templateSelect.innerHTML =
			'<option value="">Select Template...</option>';
		Object.keys(state.templateLibrary).forEach((name) => {
			const opt = document.createElement("option");
			opt.value = name;
			opt.textContent = name;
			this.templateSelect.appendChild(opt);
		});
	}

	renderMetadataUI(province) {
		this.metaList.innerHTML = "";
		if (!province.metadata) province.metadata = {};

		// Sync Color Picker
		this.provColorPicker.value = colorToHex(null, province.color);

		// Sync Owner Select
		this.provOwnerSelect.value = province.ownerId || "0";

		const keys = Object.keys(province.metadata);

		if (keys.length === 0) {
			this.metaList.innerHTML = `<div class="text-gray-500 italic text-sm">No properties set.</div>`;
			return;
		}

		keys.forEach((key) => {
			const row = document.createElement("div");
			row.className = "meta-row";

			const keyEl = document.createElement("div");
			keyEl.className = "meta-key";
			keyEl.textContent = key;

			const valInput = document.createElement("input");
			valInput.type = "text";
			valInput.className = "meta-val-input";
			valInput.value = province.metadata[key];

			valInput.addEventListener("change", (e) => {
				province.metadata[key] = e.target.value;
				this.history.saveHistory(); // Save on change
			});
			valInput.addEventListener("keydown", (e) => e.stopPropagation());

			const delBtn = document.createElement("div");
			delBtn.className = "meta-del";
			delBtn.innerHTML = "&times;";
			delBtn.title = "Remove Property";
			delBtn.onclick = () => {
				delete province.metadata[key];
				this.history.saveHistory(); // Save on delete
				this.renderMetadataUI(province);
			};

			row.appendChild(keyEl);
			row.appendChild(valInput);
			row.appendChild(delBtn);
			this.metaList.appendChild(row);
		});
	}

	deleteSelectedRef() {
		if (state.selectedRefImage) {
			state.referenceImages = state.referenceImages.filter(
				(r) => r !== state.selectedRefImage
			);
			state.selectedRefImage = null;
			this.updateUI();
			this.renderer.render();
		}
	}

	syncColorPicker(color) {
		this.provColorPicker.value = colorToHex(null, color);
	}
}
