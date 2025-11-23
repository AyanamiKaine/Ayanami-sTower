import { state } from "../core/State.js";

export class Renderer {
	constructor(canvas) {
		this.canvas = canvas;
		this.ctx = canvas.getContext("2d", { alpha: false });
		this.lastFrameTime = 0;
		this.frameCount = 0;
		this.lastFpsUpdate = 0;

		// DOM Elements for stats (optional, or passed in)
		this.fpsDisplay = document.getElementById("fpsDisplay");
		this.lodToggle = document.getElementById("lodToggle");
		this.gridToggle = document.getElementById("gridToggle");
		this.editRefsToggle = document.getElementById("editRefsToggle");
	}

	createBackgroundPattern() {
		const patternCanvas = document.createElement("canvas");
		const size = 20;
		patternCanvas.width = size;
		patternCanvas.height = size;
		const pCtx = patternCanvas.getContext("2d");

		// Background
		pCtx.fillStyle = "#0f172a"; // Slate 900
		pCtx.fillRect(0, 0, size, size);

		// Dot
		pCtx.fillStyle = "rgba(255, 255, 255, 0.05)";
		pCtx.beginPath();
		pCtx.arc(size / 2, size / 2, 1, 0, Math.PI * 2);
		pCtx.fill();

		return this.ctx.createPattern(patternCanvas, "repeat");
	}

	resize() {
		const dpr = window.devicePixelRatio || 1;
		this.canvas.width = window.innerWidth * dpr;
		this.canvas.height = window.innerHeight * dpr;
		this.canvas.style.width = `${window.innerWidth}px`;
		this.canvas.style.height = `${window.innerHeight}px`;
		if (state.isMapInitialized) this.render();
	}

	start() {
		const loop = (time) => {
			this.draw(time);
			requestAnimationFrame(loop);
		};
		requestAnimationFrame(loop);
	}

	render() {
		this.draw(performance.now());
	}

	draw(time) {
		if (time && typeof time === "number") {
			this.frameCount++;
			if (time - this.lastFpsUpdate > 1000) {
				const fps = Math.round(
					(this.frameCount * 1000) / (time - this.lastFpsUpdate)
				);
				if (this.fpsDisplay) this.fpsDisplay.textContent = `FPS: ${fps}`;
				this.lastFpsUpdate = time;
				this.frameCount = 0;
			}
		}

		if (!state.isMapInitialized) {
			this.ctx.fillStyle = "#000000";
			this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height);
			return;
		}

		const dpr = window.devicePixelRatio || 1;
		const camera = state.camera;
		const mapConfig = state.mapConfig;

		const viewMinX = -camera.x / camera.zoom;
		const viewMinY = -camera.y / camera.zoom;
		const viewMaxX = (this.canvas.width / dpr - camera.x) / camera.zoom;
		const viewMaxY = (this.canvas.height / dpr - camera.y) / camera.zoom;

		const useLOD = this.lodToggle ? this.lodToggle.checked : false;
		const lodThreshold = (1 / camera.zoom) * 2;

		// 1. Fill the Void
		if (!this.bgPattern) this.bgPattern = this.createBackgroundPattern();
		this.ctx.fillStyle = this.bgPattern;
		this.ctx.setTransform(1, 0, 0, 1, 0, 0);
		this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height);

		// 2. Apply Transforms
		this.ctx.scale(dpr, dpr);
		this.ctx.translate(camera.x, camera.y);
		this.ctx.scale(camera.zoom, camera.zoom);

		// 3. Draw Map Area
		// Drop Shadow for Map
		this.ctx.shadowColor = "rgba(0, 0, 0, 0.5)";
		this.ctx.shadowBlur = 50;
		this.ctx.shadowOffsetX = 0;
		this.ctx.shadowOffsetY = 0;

		this.ctx.fillStyle = "#1a202c";
		this.ctx.fillRect(0, 0, mapConfig.width, mapConfig.height);

		this.ctx.shadowColor = "transparent"; // Reset shadow
		this.ctx.shadowBlur = 0;

		this.ctx.strokeStyle = "#ffffff";
		this.ctx.lineWidth = 2 / camera.zoom;
		this.ctx.strokeRect(0, 0, mapConfig.width, mapConfig.height);

		// 3b. Draw Reference Images (Behind Grid/Provinces)
		state.referenceImages.forEach((ref) => {
			this.ctx.save();
			this.ctx.globalAlpha = ref.opacity;
			this.ctx.drawImage(ref.img, ref.x, ref.y, ref.width, ref.height);
			this.ctx.restore();

			// If selected in Edit Ref Mode, draw highlight
			if (
				this.editRefsToggle &&
				this.editRefsToggle.checked &&
				ref === state.selectedRefImage
			) {
				this.ctx.strokeStyle = "#fbbf24"; // Amber
				this.ctx.lineWidth = 2 / camera.zoom;
				this.ctx.strokeRect(ref.x, ref.y, ref.width, ref.height);

				// Draw resize handle (bottom right)
				const handleSize = 15 / camera.zoom;
				this.ctx.fillStyle = "#fbbf24";
				this.ctx.fillRect(
					ref.x + ref.width - handleSize / 2,
					ref.y + ref.height - handleSize / 2,
					handleSize,
					handleSize
				);
			}
		});

		// 4. Grid
		if (this.gridToggle && this.gridToggle.checked) {
			const GRID_SIZE = state.GRID_SIZE;
			const startX = Math.max(0, Math.floor(viewMinX / GRID_SIZE) * GRID_SIZE);
			const endX = Math.min(
				mapConfig.width,
				Math.floor(viewMaxX / GRID_SIZE) * GRID_SIZE + GRID_SIZE
			);
			const startY = Math.max(0, Math.floor(viewMinY / GRID_SIZE) * GRID_SIZE);
			const endY = Math.min(
				mapConfig.height,
				Math.floor(viewMaxY / GRID_SIZE) * GRID_SIZE + GRID_SIZE
			);

			this.ctx.beginPath();
			this.ctx.strokeStyle = "rgba(255, 255, 255, 0.05)";
			this.ctx.lineWidth = 1 / camera.zoom;
			for (let x = startX; x <= endX; x += GRID_SIZE) {
				this.ctx.moveTo(x, startY);
				this.ctx.lineTo(x, endY);
			}
			for (let y = startY; y <= endY; y += GRID_SIZE) {
				this.ctx.moveTo(startX, y);
				this.ctx.lineTo(endX, y);
			}
			this.ctx.stroke();
		}

		// 5. Provinces (Optimized)
		let visibleCount = 0;
		const standardLineWidth = 2 / camera.zoom;
		const selectedLineWidth = 3 / camera.zoom;

		// Pass 1: Draw all provinces (Fills & Standard Borders)
		state.provinces.forEach((province) => {
			if (province.points.length < 1) return;

			// CULLING CHECK
			const bbox = province.bbox;
			if (!bbox) {
				state.updateProvinceBounds(province);
			}
			if (
				province.bbox.maxX < viewMinX ||
				province.bbox.minX > viewMaxX ||
				province.bbox.maxY < viewMinY ||
				province.bbox.minY > viewMaxY
			) {
				return; // Skip
			}

			visibleCount++;

			this.ctx.beginPath();

			if (useLOD) {
				const pts = province.points;
				this.ctx.moveTo(pts[0].x, pts[0].y);
				let lastDrawnX = pts[0].x;
				let lastDrawnY = pts[0].y;

				for (let i = 1; i < pts.length; i++) {
					const pt = pts[i];
					const dist =
						Math.abs(pt.x - lastDrawnX) + Math.abs(pt.y - lastDrawnY);
					if (dist > lodThreshold) {
						this.ctx.lineTo(pt.x, pt.y);
						lastDrawnX = pt.x;
						lastDrawnY = pt.y;
					}
				}
			} else {
				this.ctx.moveTo(province.points[0].x, province.points[0].y);
				for (let i = 1; i < province.points.length; i++) {
					this.ctx.lineTo(province.points[i].x, province.points[i].y);
				}
			}

			this.ctx.closePath();

			// Determine Fill Color based on Map Mode
			let fillColor = province.color;
			if (state.mapMode === "owner") {
				const owner =
					state.owners.find((o) => o.id === province.ownerId) ||
					state.owners[0];
				fillColor = owner.color;
			}
			this.ctx.fillStyle = fillColor;
			this.ctx.fill();

			this.ctx.lineJoin = "round";
			this.ctx.strokeStyle = "#000000";
			this.ctx.lineWidth = standardLineWidth;
			this.ctx.stroke();

			if (state.appMode === "edit") {
				this.ctx.fillStyle = "rgba(255,255,255,0.3)";
				if (camera.zoom > 0.5) {
					for (const pt of province.points) {
						this.ctx.beginPath();
						this.ctx.arc(pt.x, pt.y, standardLineWidth, 0, Math.PI * 2);
						this.ctx.fill();
					}
				}
			}
		});

		// Pass 2: Draw Selected Highlights (Always on Top)
		if (state.selectedProvinces.size > 0) {
			state.selectedProvinces.forEach((province) => {
				if (province.points.length < 1) return;

				// Culling for selection
				if (
					province.bbox.maxX < viewMinX ||
					province.bbox.minX > viewMaxX ||
					province.bbox.maxY < viewMinY ||
					province.bbox.minY > viewMaxY
				) {
					return;
				}

				this.ctx.beginPath();
				if (useLOD) {
					const pts = province.points;
					this.ctx.moveTo(pts[0].x, pts[0].y);
					let lastDrawnX = pts[0].x;
					let lastDrawnY = pts[0].y;

					for (let i = 1; i < pts.length; i++) {
						const pt = pts[i];
						const dist =
							Math.abs(pt.x - lastDrawnX) + Math.abs(pt.y - lastDrawnY);
						if (dist > lodThreshold) {
							this.ctx.lineTo(pt.x, pt.y);
							lastDrawnX = pt.x;
							lastDrawnY = pt.y;
						}
					}
				} else {
					this.ctx.moveTo(province.points[0].x, province.points[0].y);
					for (let i = 1; i < province.points.length; i++) {
						this.ctx.lineTo(province.points[i].x, province.points[i].y);
					}
				}
				this.ctx.closePath();

				this.ctx.lineJoin = "round";
				this.ctx.shadowColor = "white";
				this.ctx.shadowBlur = 15;
				this.ctx.strokeStyle = "white";
				this.ctx.lineWidth = selectedLineWidth;
				this.ctx.stroke();
				this.ctx.shadowBlur = 0;
			});
		}

		// Pass 3: Draw Owner Labels
		if (state.mapMode === "owner") {
			// Zoom fade logic
			const ZOOM_FADE_START = 2.5;
			const ZOOM_FADE_END = 5.0;
			let opacity = 1;

			if (camera.zoom > ZOOM_FADE_START) {
				opacity =
					1 -
					(camera.zoom - ZOOM_FADE_START) / (ZOOM_FADE_END - ZOOM_FADE_START);
				opacity = Math.max(0, Math.min(1, opacity));
			}

			if (opacity > 0) {
				if (state.ownerBoundsDirty) state.updateOwnerClusters();

				this.ctx.save();
				this.ctx.globalAlpha = opacity;
				this.ctx.textAlign = "center";
				this.ctx.textBaseline = "middle";
				this.ctx.lineWidth = 3 / camera.zoom;
				this.ctx.shadowColor = "black";
				this.ctx.shadowBlur = 4;

				for (const [id, clusters] of Object.entries(state.ownerClusters)) {
					const owner = state.owners.find((o) => o.id === id);
					if (!owner) continue;

					// Dynamic styling: Tinted white fill, Darkened outline
					this.ctx.fillStyle = `color-mix(in srgb, ${owner.color}, white 85%)`;
					this.ctx.strokeStyle = `color-mix(in srgb, ${owner.color}, black 60%)`;

					clusters.forEach((bbox) => {
						const width = bbox.maxX - bbox.minX;
						const height = bbox.maxY - bbox.minY;
						const cx = bbox.minX + width / 2;
						const cy = bbox.minY + height / 2;

						// Font size based on cluster size
						const fontSize = Math.min(width, height) / 4;
						if (fontSize < 10 / camera.zoom) return; // Too small

						this.ctx.font = `bold ${fontSize}px sans-serif`;
						this.ctx.strokeText(owner.name, cx, cy);
						this.ctx.fillText(owner.name, cx, cy);
					});
				}
				this.ctx.restore();
			}
		}

		// 6. Draft Points (Drawing Mode)
		if (state.appMode === "draw" && state.draftPoints.length > 0) {
			this.ctx.beginPath();
			this.ctx.moveTo(state.draftPoints[0].x, state.draftPoints[0].y);
			for (let i = 1; i < state.draftPoints.length; i++) {
				this.ctx.lineTo(state.draftPoints[i].x, state.draftPoints[i].y);
			}
			// Draw line to mouse cursor if active
			// We need mousePos here, but it's in InputManager or State?
			// Let's assume we just draw what we have.
			// The original code didn't seem to draw a line to the cursor explicitly in the draftPoints loop,
			// but it might have been implied or I missed it.
			// Actually, the original code just drew the draftPoints lines.

			this.ctx.strokeStyle = "#fbbf24"; // Amber
			this.ctx.lineWidth = 2 / camera.zoom;
			this.ctx.stroke();

			// Draw points
			this.ctx.fillStyle = "#fbbf24";
			for (const pt of state.draftPoints) {
				this.ctx.beginPath();
				this.ctx.arc(pt.x, pt.y, 4 / camera.zoom, 0, Math.PI * 2);
				this.ctx.fill();
			}
		}

		// 7. Hover Highlights
		if (state.hoveredVertex) {
			this.ctx.beginPath();
			this.ctx.arc(
				state.hoveredVertex.x,
				state.hoveredVertex.y,
				6 / camera.zoom,
				0,
				Math.PI * 2
			);
			this.ctx.fillStyle = "#ef4444"; // Red
			this.ctx.fill();
			this.ctx.strokeStyle = "white";
			this.ctx.lineWidth = 2 / camera.zoom;
			this.ctx.stroke();
		} else if (state.hoveredSegment) {
			this.ctx.beginPath();
			this.ctx.arc(
				state.hoveredSegment.x,
				state.hoveredSegment.y,
				5 / camera.zoom,
				0,
				Math.PI * 2
			);
			this.ctx.fillStyle = "#3b82f6"; // Blue
			this.ctx.fill();
		}

		// 8. Box Selection
		if (state.isBoxSelecting) {
			// We need mousePos in world coordinates or screen coordinates.
			// state.boxSelectionStart is in world coordinates.
			// We need the current mouse position in world coordinates.
			// This is tricky because `render` doesn't receive mouse input.
			// However, `state` could track `worldPos` (current mouse world pos).
			// Let's assume `state.worldPos` exists or we need to add it.
			// I'll add `worldPos` to State.

			// Wait, I didn't add `worldPos` to State yet. I should.
			if (state.worldPos) {
				const x = state.boxSelectionStart.x;
				const y = state.boxSelectionStart.y;
				const w = state.worldPos.x - x;
				const h = state.worldPos.y - y;

				this.ctx.fillStyle = "rgba(59, 130, 246, 0.2)";
				this.ctx.fillRect(x, y, w, h);
				this.ctx.strokeStyle = "#3b82f6";
				this.ctx.lineWidth = 1 / camera.zoom;
				this.ctx.strokeRect(x, y, w, h);
			}
		}
	}
}
