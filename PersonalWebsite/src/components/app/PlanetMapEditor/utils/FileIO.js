import { state } from "../core/State.js";

export function parseMapData(jsonString) {
	try {
		const data = JSON.parse(jsonString);

		if (!data.map || !data.provinces) {
			throw new Error("Invalid Map JSON file.");
		}

		return data;
	} catch (err) {
		throw err;
	}
}

export function generateExportData() {
	return {
		map: state.mapConfig,
		templates: state.templateLibrary,
		owners: state.owners,
		provinces: state.provinces.map((p) => ({
			color: p.color,
			ownerId: p.ownerId || "0",
			metadata: p.metadata,
			points: p.points.map((pt) => ({
				x: Math.round(pt.x * 100) / 100,
				y: Math.round(pt.y * 100) / 100,
			})),
		})),
	};
}
