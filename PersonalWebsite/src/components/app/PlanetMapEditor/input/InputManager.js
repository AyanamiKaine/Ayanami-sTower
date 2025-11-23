import { state } from "../core/State.js";
import {
	toWorld,
	findClosestVertex,
	findProvinceAt,
	findClosestSegment,
	getProvincesInBox,
	getRefImageAt,
	isOverRefHandle,
} from "../core/Query.js";
import {
	finishShape,
	undoLastPoint,
	deleteSelectedProvinces,
} from "../core/Operations.js";
import { clamp } from "../utils/Geometry.js";
import { colorToHex } from "../utils/Colors.js";

export class InputManager {
	constructor(canvas, history, renderer, uiManager) {
		this.canvas = canvas;
		this.history = history;
		this.renderer = renderer;
		this.uiManager = uiManager;

		this.setupEventListeners();
	}

	setupEventListeners() {
		this.canvas.addEventListener("wheel", this.handleWheel.bind(this), {
			passive: false,
		});
		this.canvas.addEventListener("mousemove", this.handleMouseMove.bind(this));
		this.canvas.addEventListener("mousedown", this.handleMouseDown.bind(this));
		this.canvas.addEventListener("mouseup", this.handleMouseUp.bind(this));
		this.canvas.addEventListener("dblclick", this.handleDoubleClick.bind(this));
		this.canvas.addEventListener(
			"mouseleave",
			this.handleMouseLeave.bind(this)
		);
		window.addEventListener("keydown", this.handleKeyDown.bind(this));
	}

	handleWheel(e) {
		if (!state.isMapInitialized) return;
		e.preventDefault();
		const zoomSensitivity = 0.001;
		const delta = -e.deltaY * zoomSensitivity;
		const newZoom = Math.min(
			Math.max(0.1, state.camera.zoom * (1 + delta)),
			10
		);
		const mouseWorldBefore = toWorld(state.mousePos.x, state.mousePos.y);
		state.camera.zoom = newZoom;
		state.camera.x = state.mousePos.x - mouseWorldBefore.x * state.camera.zoom;
		state.camera.y = state.mousePos.y - mouseWorldBefore.y * state.camera.zoom;
		this.uiManager.updateUI();
		this.renderer.render();
	}

	handleMouseMove(e) {
		if (!state.isMapInitialized) return;
		const rect = this.canvas.getBoundingClientRect();
		state.mousePos.x = e.clientX - rect.left;
		state.mousePos.y = e.clientY - rect.top;

		if (state.isPanning) {
			const dx = e.clientX - state.lastPanPos.x;
			const dy = e.clientY - state.lastPanPos.y;
			state.camera.x += dx;
			state.camera.y += dy;
			state.lastPanPos = { x: e.clientX, y: e.clientY };
			this.renderer.render();
			return;
		}

		let rawWorld = toWorld(state.mousePos.x, state.mousePos.y);
		state.worldPos.x = clamp(rawWorld.x, 0, state.mapConfig.width);
		state.worldPos.y = clamp(rawWorld.y, 0, state.mapConfig.height);

		// --- REFERENCE EDIT MODE ---
		if (state.isEditRefsMode) {
			const cursorWorld = toWorld(state.mousePos.x, state.mousePos.y);

			if (
				state.isDraggingRef &&
				state.selectedRefImage &&
				!state.isResizingRef
			) {
				state.selectedRefImage.x += cursorWorld.x - state.dragStartWorldPos.x;
				state.selectedRefImage.y += cursorWorld.y - state.dragStartWorldPos.y;
				state.dragStartWorldPos = { x: cursorWorld.x, y: cursorWorld.y };
				this.renderer.render();
				return;
			}

			if (state.isResizingRef && state.selectedRefImage) {
				const dx = cursorWorld.x - state.dragStartWorldPos.x;
				const dy = cursorWorld.y - state.dragStartWorldPos.y;

				const newW = Math.max(10, state.selectedRefImage.width + dx);
				const newH = Math.max(10, state.selectedRefImage.height + dy);

				state.selectedRefImage.width = newW;
				state.selectedRefImage.height = newH;

				state.dragStartWorldPos = { x: cursorWorld.x, y: cursorWorld.y };
				this.renderer.render();
				return;
			}

			// Hover logic for refs
			const hoverRef = getRefImageAt(cursorWorld.x, cursorWorld.y);
			if (
				state.selectedRefImage &&
				isOverRefHandle(cursorWorld.x, cursorWorld.y, state.selectedRefImage)
			) {
				this.canvas.style.cursor = "nwse-resize";
			} else if (hoverRef) {
				this.canvas.style.cursor = "move";
			} else {
				this.canvas.style.cursor = "default";
			}

			return; // Skip province logic
		}

		// --- PROVINCE EDIT MODE ---
		if (state.appMode === "edit" || state.appMode === "select") {
			if (state.isBoxSelecting) {
				this.renderer.render();
				return;
			}

			if (state.appMode === "edit") {
				if (state.isDraggingVertex && state.draggedVerticesGroup.length > 0) {
					this.history.isDirty = true; // Mark as changed
					const excludeSet = new Set(state.draggedVerticesGroup);
					const snapTo = findClosestVertex(
						state.worldPos.x,
						state.worldPos.y,
						excludeSet
					);

					state.activeSnapTarget = snapTo;
					const targetX = snapTo ? snapTo.x : state.worldPos.x;
					const targetY = snapTo ? snapTo.y : state.worldPos.y;

					state.draggedVerticesGroup.forEach((pt) => {
						pt.x = targetX;
						pt.y = targetY;
					});
					this.renderer.render();
					return;
				}

				if (state.isDraggingProvince && state.selectedProvinces.size > 0) {
					this.history.isDirty = true; // Mark as changed
					let dx = state.worldPos.x - state.dragStartWorldPos.x;
					let dy = state.worldPos.y - state.dragStartWorldPos.y;

					let minX = Infinity,
						minY = Infinity,
						maxX = -Infinity,
						maxY = -Infinity;
					state.selectedProvinces.forEach((prov) => {
						if (prov.bbox.minX < minX) minX = prov.bbox.minX;
						if (prov.bbox.maxX > maxX) maxX = prov.bbox.maxX;
						if (prov.bbox.minY < minY) minY = prov.bbox.minY;
						if (prov.bbox.maxY > maxY) maxY = prov.bbox.maxY;
					});

					if (minX + dx < 0) dx = -minX;
					if (minY + dy < 0) dy = -minY;
					if (maxX + dx > state.mapConfig.width)
						dx = state.mapConfig.width - maxX;
					if (maxY + dy > state.mapConfig.height)
						dy = state.mapConfig.height - maxY;

					state.selectedProvinces.forEach((prov) => {
						prov.points.forEach((p) => {
							p.x += dx;
							p.y += dy;
						});
						prov.bbox.minX += dx;
						prov.bbox.maxX += dx;
						prov.bbox.minY += dy;
						prov.bbox.maxY += dy;
					});

					state.dragStartWorldPos.x += dx;
					state.dragStartWorldPos.y += dy;

					state.ownerBoundsDirty = true; // Update labels during drag

					this.renderer.render();
					return;
				}
			}

			if (!state.isDraggingVertex && !state.isDraggingProvince) {
				state.activeSnapTarget = null;
				state.hoveredVertex = findClosestVertex(
					state.worldPos.x,
					state.worldPos.y
				);

				if (state.hoveredVertex) {
					state.hoveredSegment = null;
					this.canvas.style.cursor =
						state.appMode === "edit" ? "move" : "default";
				} else {
					state.hoveredSegment = findClosestSegment(
						state.worldPos.x,
						state.worldPos.y
					);
					if (state.hoveredSegment && state.appMode === "edit") {
						this.canvas.style.cursor = "copy";
					} else {
						const hoveredProv = findProvinceAt(
							state.worldPos.x,
							state.worldPos.y
						);
						this.canvas.style.cursor = hoveredProv ? "pointer" : "default";
					}
				}
			}
		} else {
			state.hoveredVertex = findClosestVertex(
				state.worldPos.x,
				state.worldPos.y
			);
			this.canvas.style.cursor = state.hoveredVertex
				? "crosshair"
				: "crosshair";
		}
		this.renderer.render();
	}

	handleMouseDown(e) {
		if (!state.isMapInitialized) return;
		if (e.button === 1) {
			state.isPanning = true;
			state.lastPanPos = { x: e.clientX, y: e.clientY };
			this.canvas.style.cursor = "grabbing";
			e.preventDefault();
			return;
		}
		if (e.button !== 0) return;

		// Eyedropper Shortcut (Alt + Click)
		if (
			e.altKey &&
			(state.appMode === "edit" || state.appMode === "select") &&
			!state.isEditRefsMode
		) {
			const rawWorld = toWorld(state.mousePos.x, state.mousePos.y);
			const wX = clamp(rawWorld.x, 0, state.mapConfig.width);
			const wY = clamp(rawWorld.y, 0, state.mapConfig.height);
			const clickedProv = findProvinceAt(wX, wY);

			if (clickedProv && state.selectedProvinces.size === 1) {
				const target = state.selectedProvinces.values().next().value;
				target.color = clickedProv.color;
				// We need to update the color picker UI.
				// This is a bit tricky as InputManager shouldn't know about specific UI elements.
				// But we can call uiManager.updateUI() which should handle it.
				// Or we can emit an event.
				// For now, let's assume uiManager handles it or we access the DOM directly (bad but quick).
				// Better: uiManager.syncColorPicker(target.color);
				if (this.uiManager.syncColorPicker)
					this.uiManager.syncColorPicker(target.color);

				this.history.saveHistory();
				this.renderer.render();
				return;
			}
		}

		this.history.isDirty = false; // Reset dirty flag

		// --- Reference Edit Mode Click ---
		if (state.isEditRefsMode) {
			const cursorWorld = toWorld(state.mousePos.x, state.mousePos.y);

			// Check Handle First
			if (
				state.selectedRefImage &&
				isOverRefHandle(cursorWorld.x, cursorWorld.y, state.selectedRefImage)
			) {
				state.isResizingRef = true;
				state.dragStartWorldPos = { x: cursorWorld.x, y: cursorWorld.y };
				return;
			}

			// Check Body
			const clickedRef = getRefImageAt(cursorWorld.x, cursorWorld.y);
			if (clickedRef) {
				state.selectedRefImage = clickedRef;
				state.isDraggingRef = true;
				state.dragStartWorldPos = { x: cursorWorld.x, y: cursorWorld.y };
				this.uiManager.updateUI();
				this.renderer.render();
			} else {
				state.selectedRefImage = null;
				this.uiManager.updateUI();
				this.renderer.render();
			}
			return;
		}

		// --- Province Modes ---
		if (state.appMode === "draw") {
			if (state.hoveredVertex) {
				if (
					state.draftPoints.length > 2 &&
					state.hoveredVertex === state.draftPoints[0]
				) {
					finishShape(this.history);
					this.uiManager.updateUI();
					this.renderer.render();
				} else {
					state.draftPoints.push({
						x: state.hoveredVertex.x,
						y: state.hoveredVertex.y,
					});
				}
			} else {
				state.draftPoints.push({ x: state.worldPos.x, y: state.worldPos.y });
			}
		} else {
			const shiftKey = e.shiftKey;
			if (state.hoveredVertex && state.appMode === "edit") {
				state.isDraggingVertex = true;
				state.draggedVerticesGroup = [];
				state.provinces.forEach((prov) => {
					prov.points.forEach((pt) => {
						if (
							pt.x === state.hoveredVertex.x &&
							pt.y === state.hoveredVertex.y
						) {
							state.draggedVerticesGroup.push(pt);
						}
					});
				});
			} else if (!state.hoveredSegment) {
				const clickedProv = findProvinceAt(state.worldPos.x, state.worldPos.y);
				if (clickedProv) {
					if (state.appMode === "edit") {
						state.isDraggingProvince = true;
					}
					state.dragStartWorldPos = {
						x: state.worldPos.x,
						y: state.worldPos.y,
					};
					if (shiftKey) {
						if (state.selectedProvinces.has(clickedProv)) {
							state.selectedProvinces.delete(clickedProv);
							state.isDraggingProvince = false;
						} else {
							state.selectedProvinces.add(clickedProv);
						}
					} else {
						if (!state.selectedProvinces.has(clickedProv)) {
							state.selectedProvinces.clear();
							state.selectedProvinces.add(clickedProv);
						}
					}
				} else {
					if (!shiftKey) state.selectedProvinces.clear();
					state.isBoxSelecting = true;
					state.boxSelectionStart = {
						x: state.worldPos.x,
						y: state.worldPos.y,
					};
				}
			}
		}
		this.uiManager.updateUI();
		this.renderer.render();
	}

	handleMouseUp() {
		// Ref Logic
		if (state.isDraggingRef || state.isResizingRef) {
			state.isDraggingRef = false;
			state.isResizingRef = false;
			this.renderer.render();
			return;
		}

		if (state.isBoxSelecting) {
			const boxProvs = getProvincesInBox(
				state.boxSelectionStart.x,
				state.boxSelectionStart.y,
				state.worldPos.x,
				state.worldPos.y
			);
			boxProvs.forEach((p) => state.selectedProvinces.add(p));
			state.isBoxSelecting = false;
		}

		if (state.isDraggingVertex && state.draggedVerticesGroup.length > 0) {
			state.provinces.forEach((prov) => {
				const hasDraggedPt = prov.points.some((p) =>
					state.draggedVerticesGroup.includes(p)
				);
				if (hasDraggedPt) state.updateProvinceBounds(prov);
			});
		}

		// --- UNDO LOGIC ---
		if (state.appMode === "edit" && this.history.isDirty) {
			this.history.saveHistory(); // Save state if something moved
		}

		state.isPanning = false;
		state.isDraggingVertex = false;
		state.isDraggingProvince = false;
		state.draggedVerticesGroup = [];
		state.activeSnapTarget = null;
		this.history.isDirty = false;

		if (state.appMode === "edit" && !state.isEditRefsMode) {
			this.canvas.style.cursor = state.hoveredVertex ? "move" : "default";
		} else {
			this.canvas.style.cursor = "crosshair";
		}
		this.uiManager.updateUI();
		this.renderer.render();
	}

	handleDoubleClick(e) {
		if (state.appMode !== "edit" || state.isEditRefsMode) return;
		e.preventDefault();

		if (state.hoveredVertex) {
			let canDelete = true;
			state.provinces.forEach((prov) => {
				if (
					prov.points.includes(state.hoveredVertex) &&
					prov.points.length <= 3
				)
					canDelete = false;
			});
			if (!canDelete) return;

			const targetX = state.hoveredVertex.x;
			const targetY = state.hoveredVertex.y;

			state.provinces.forEach((prov) => {
				const lenBefore = prov.points.length;
				prov.points = prov.points.filter(
					(pt) => pt.x !== targetX || pt.y !== targetY
				);
				if (prov.points.length !== lenBefore) state.updateProvinceBounds(prov);
			});
			state.hoveredVertex = null;
			this.history.saveHistory(); // Save state on vertex delete
			this.renderer.render();
			this.uiManager.updateUI();
			return;
		}

		if (state.hoveredSegment) {
			const { x, y, insertTargets } = state.hoveredSegment;
			insertTargets.forEach((target) => {
				target.province.points.splice(target.index, 0, { x, y });
				state.updateProvinceBounds(target.province);
			});
			state.hoveredSegment = null;
			this.history.saveHistory(); // Save state on vertex add
			this.renderer.render();
			this.uiManager.updateUI();
		}
	}

	handleMouseLeave() {
		state.isPanning = false;
		state.isDraggingVertex = false;
		state.isDraggingProvince = false;
		state.isDraggingRef = false;
		state.isResizingRef = false;
		state.isBoxSelecting = false;
	}

	handleKeyDown(e) {
		if (!state.isMapInitialized) return;

		// CRITICAL FIX: Ignore global shortcuts
		if (e.target.tagName === "INPUT" || e.target.tagName === "TEXTAREA") return;

		// Undo/Redo Shortcuts
		if ((e.metaKey || e.ctrlKey) && (e.key === "z" || e.key === "Z")) {
			e.preventDefault();
			if (e.shiftKey) {
				this.history.redo();
			} else {
				this.history.undo();
			}
		}
		if ((e.metaKey || e.ctrlKey) && (e.key === "y" || e.key === "Y")) {
			e.preventDefault();
			this.history.redo();
		}

		if (state.appMode === "draw") {
			if (e.key === "Enter") {
				finishShape(this.history);
				this.uiManager.updateUI();
				this.renderer.render();
			}
			if (e.key === "Escape") {
				state.draftPoints = [];
				this.renderer.render();
				this.uiManager.updateUI();
			}
		} else if (state.appMode === "edit" || state.appMode === "select") {
			if (e.key === "Delete" || e.key === "Backspace") {
				if (state.isEditRefsMode && state.selectedRefImage) {
					// We need to trigger deleteRef.
					// This is a UI action.
					// We can call uiManager.deleteSelectedRef()
					if (this.uiManager.deleteSelectedRef)
						this.uiManager.deleteSelectedRef();
				} else {
					deleteSelectedProvinces(this.history);
					this.uiManager.updateUI();
					this.renderer.render();
				}
			}
		}
		if (e.key === "e" || e.key === "E") this.uiManager.setMode("edit");
		if (e.key === "d" || e.key === "D") this.uiManager.setMode("draw");
		if (e.key === "s" || e.key === "S") this.uiManager.setMode("select");
	}
}
