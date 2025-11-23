import { state } from "./State.js";

export class HistoryManager {
	constructor(callbacks) {
		this.MAX_HISTORY_STEPS = 50;
		this.historyStack = [];
		this.historyIndex = -1;
		this.isDirty = false;
		this.callbacks = callbacks || {}; // { onUpdate: () => {} }
	}

	saveHistory() {
		// Remove any redo steps if we are in the middle of history
		if (this.historyIndex < this.historyStack.length - 1) {
			this.historyStack = this.historyStack.slice(0, this.historyIndex + 1);
		}

		// Save deep copy of state
		const snapshot = JSON.stringify({
			provinces: state.provinces,
			owners: state.owners,
		});
		this.historyStack.push(snapshot);
		this.historyIndex++;

		// Cap size
		if (this.historyStack.length > this.MAX_HISTORY_STEPS) {
			this.historyStack.shift();
			this.historyIndex--;
		}
		state.ownerBoundsDirty = true;

		if (this.callbacks.onUpdate) this.callbacks.onUpdate();
	}

	undo() {
		if (state.appMode === "draw" && state.draftPoints.length > 0) {
			// This logic was in the main file, but it's better handled by the input manager or a specific draw manager.
			// For now, we'll handle it here or let the caller handle it.
			// But wait, undoLastPoint is specific to drawing.
			// The original undo() function handled both.
			// I will separate them. The caller should decide which undo to call.
			return false;
		}

		if (this.historyIndex > 0) {
			this.historyIndex--;
			this.loadHistoryState(this.historyStack[this.historyIndex]);
			return true;
		}
		return false;
	}

	redo() {
		if (this.historyIndex < this.historyStack.length - 1) {
			this.historyIndex++;
			this.loadHistoryState(this.historyStack[this.historyIndex]);
			return true;
		}
		return false;
	}

	loadHistoryState(jsonState) {
		try {
			const data = JSON.parse(jsonState);

			// Handle legacy history (array only) vs new object format
			if (Array.isArray(data)) {
				state.provinces = data;
				// Keep existing owners if loading legacy state
			} else {
				state.provinces = data.provinces;
				state.owners = data.owners || state.owners;
			}

			// Re-calc bounds
			state.recalculateAllBounds();
			// Clear selection to avoid bugs
			state.selectedProvinces.clear();
			state.hoveredVertex = null;

			state.ownerBoundsDirty = true;

			if (this.callbacks.onUpdate) this.callbacks.onUpdate();
		} catch (e) {
			console.error("Failed to load history state", e);
		}
	}
}
