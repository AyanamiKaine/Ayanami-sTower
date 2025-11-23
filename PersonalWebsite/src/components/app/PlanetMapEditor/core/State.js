import { areProvincesConnected } from "../utils/Geometry.js";

class State {
	constructor() {
		this.appMode = "draw";
		this.mapMode = "province"; // 'province' or 'owner'
		this.isMapInitialized = false;
		this.mapConfig = { width: 0, height: 0 };

		// Data Model
		this.provinces = [];
		this.owners = [{ id: "0", name: "Unclaimed", color: "#9ca3af" }];
		this.ownerClusters = {}; // Cache for owner clusters
		this.ownerBoundsDirty = true;
		this.draftPoints = [];
		this.referenceImages = []; // { id, img, x, y, width, height, opacity }

		this.templateLibrary = {
			"Standard City": { Type: "Urban", Population: "5000", Defense: "Low" },
			Fortress: { Type: "Military", Population: "1000", Defense: "High" },
			Wilderness: { Type: "Nature", Biome: "Forest", Population: "0" },
		};

		// Interaction State
		this.selectedProvinces = new Set();
		this.selectedRefImage = null;
		this.hoveredVertex = null;
		this.hoveredSegment = null;

		this.isDraggingVertex = false;
		this.isDraggingProvince = false;
		this.isDraggingRef = false;
		this.isResizingRef = false;

		this.dragStartWorldPos = { x: 0, y: 0 };
		this.draggedVerticesGroup = [];
		this.activeSnapTarget = null;

		this.isBoxSelecting = false;
		this.boxSelectionStart = { x: 0, y: 0 };
		this.isEditRefsMode = false;

		// Camera State
		this.camera = { x: 0, y: 0, zoom: 1 };
		this.isPanning = false;
		this.lastPanPos = { x: 0, y: 0 };

		// Config
		this.SNAP_RADIUS = 15;
		this.GRID_SIZE = 50;
		this.SEGMENT_HOVER_DIST = 10;

		this.mousePos = { x: 0, y: 0 };
		this.worldPos = { x: 0, y: 0 };
		this.hoveredVertex = null;
	}

	updateProvinceBounds(province) {
		let minX = Infinity,
			maxX = -Infinity,
			minY = Infinity,
			maxY = -Infinity;
		for (const p of province.points) {
			if (p.x < minX) minX = p.x;
			if (p.x > maxX) maxX = p.x;
			if (p.y < minY) minY = p.y;
			if (p.y > maxY) maxY = p.y;
		}
		province.bbox = { minX, maxX, minY, maxY };
	}

	recalculateAllBounds() {
		this.provinces.forEach((p) => this.updateProvinceBounds(p));
	}

	updateOwnerClusters() {
		this.ownerClusters = {};

		// Group provinces by owner
		const provincesByOwner = {};
		this.provinces.forEach((p) => {
			if (p.ownerId === "0") return;
			if (!provincesByOwner[p.ownerId]) provincesByOwner[p.ownerId] = [];
			provincesByOwner[p.ownerId].push(p);
		});

		// For each owner, find clusters
		for (const [ownerId, provs] of Object.entries(provincesByOwner)) {
			const clusters = [];
			const visited = new Set();

			for (let i = 0; i < provs.length; i++) {
				if (visited.has(i)) continue;

				// Start a new cluster
				const clusterProvs = [provs[i]];
				visited.add(i);

				// BFS to find all connected provinces
				const queue = [provs[i]];
				while (queue.length > 0) {
					const current = queue.shift();

					for (let j = 0; j < provs.length; j++) {
						if (visited.has(j)) continue;

						if (areProvincesConnected(current, provs[j])) {
							visited.add(j);
							clusterProvs.push(provs[j]);
							queue.push(provs[j]);
						}
					}
				}

				// Calculate bbox for this cluster
				let minX = Infinity,
					maxX = -Infinity,
					minY = Infinity,
					maxY = -Infinity;
				clusterProvs.forEach((p) => {
					minX = Math.min(minX, p.bbox.minX);
					maxX = Math.max(maxX, p.bbox.maxX);
					minY = Math.min(minY, p.bbox.minY);
					maxY = Math.max(maxY, p.bbox.maxY);
				});

				clusters.push({ minX, maxX, minY, maxY });
			}
			this.ownerClusters[ownerId] = clusters;
		}
		this.ownerBoundsDirty = false;
	}
}

export const state = new State();
