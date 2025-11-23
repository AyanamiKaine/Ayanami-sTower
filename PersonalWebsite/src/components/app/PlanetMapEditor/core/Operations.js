import { state } from "./State.js";
import { getRandomColor } from "../utils/Colors.js";
import { clamp } from "../utils/Geometry.js";

export function finishShape(history) {
	if (state.draftPoints.length < 3) return;
	const newProv = {
		color: getRandomColor(),
		ownerId: "0",
		points: [...state.draftPoints],
		metadata: {}, // Init metadata
	};
	state.updateProvinceBounds(newProv);
	state.provinces.push(newProv);
	state.draftPoints = [];
	history.saveHistory(); // Save state on new shape
}

export function undoLastPoint() {
	if (state.draftPoints.length > 0) {
		state.draftPoints.pop();
	}
}

export function deleteSelectedProvinces(history) {
	if (state.selectedProvinces.size > 0) {
		state.provinces = state.provinces.filter(
			(p) => !state.selectedProvinces.has(p)
		);
		state.selectedProvinces.clear();
		history.saveHistory(); // Save state on delete
	}
}

export function clearCanvas(history) {
	if (confirm("Delete all provinces?")) {
		state.provinces = [];
		state.draftPoints = [];
		state.selectedProvinces.clear();
		history.saveHistory(); // Save state on clear
	}
}

export function initializeMap(w, h, history) {
	state.mapConfig.width = w;
	state.mapConfig.height = h;

	// We need canvas to calculate camera.
	// But Operations shouldn't know about canvas.
	// We can calculate camera in UIManager or pass canvas dimensions.
	// Or we can just set default camera and let Renderer/UIManager adjust it.

	// Let's assume we just set the map config and reset state.
	state.isMapInitialized = true;

	// Reset History
	state.historyStack = []; // Wait, history is managed by HistoryManager.
	// We should call history.reset() or similar.
	// But history.saveHistory() clears redo stack.
	// We need to clear undo stack too.
	// We can access history object passed in.
	if (history) {
		history.historyStack = [];
		history.historyIndex = -1;
		history.saveHistory();
	}
}

export function generateRandomProvinces(count, width, height) {
	state.provinces = [];
	state.draftPoints = [];
	state.selectedProvinces.clear();

	const w = width;
	const h = height;
	const padding = 20;

	for (let i = 0; i < count; i++) {
		const cx = padding + Math.random() * (w - padding * 2);
		const cy = padding + Math.random() * (h - padding * 2);

		const points = [];
		const sides = 5 + Math.floor(Math.random() * 4);
		const radius = 15 + Math.random() * 35;

		for (let j = 0; j < sides; j++) {
			const angle = (j / sides) * Math.PI * 2;
			const r = radius * (0.7 + Math.random() * 0.6);
			points.push({
				x: clamp(cx + Math.cos(angle) * r, 0, w),
				y: clamp(cy + Math.sin(angle) * r, 0, h),
			});
		}

		const newProv = {
			color: getRandomColor(),
			ownerId: "0",
			points: points,
			metadata: {}, // Init empty metadata
		};
		state.updateProvinceBounds(newProv);
		state.provinces.push(newProv);
	}
}
