import { state } from "./State.js";
import {
	getDistance,
	pointInPolygon,
	distToSegmentSquared,
	getProjectedPoint,
} from "../utils/Geometry.js";

export function toWorld(screenX, screenY) {
	return {
		x: (screenX - state.camera.x) / state.camera.zoom,
		y: (screenY - state.camera.y) / state.camera.zoom,
	};
}

export function findClosestVertex(worldX, worldY, excludeSet = null) {
	let closest = null;
	let minDst = Infinity;
	const maxDist = state.SNAP_RADIUS / state.camera.zoom;

	for (const province of state.provinces) {
		const pad = maxDist + 10;
		if (
			worldX < province.bbox.minX - pad ||
			worldX > province.bbox.maxX + pad ||
			worldY < province.bbox.minY - pad ||
			worldY > province.bbox.maxY + pad
		)
			continue;

		for (const pt of province.points) {
			if (excludeSet && excludeSet.has(pt)) continue;
			const dst = getDistance({ x: worldX, y: worldY }, pt);
			if (dst < minDst) {
				minDst = dst;
				closest = pt;
			}
		}
	}

	if (state.appMode === "draw" && state.draftPoints.length > 0) {
		const dst = getDistance({ x: worldX, y: worldY }, state.draftPoints[0]);
		if (dst < minDst) {
			minDst = dst;
			closest = state.draftPoints[0];
		}
	}

	if (minDst <= maxDist) return closest;
	return null;
}

export function findProvinceAt(worldX, worldY) {
	for (let i = state.provinces.length - 1; i >= 0; i--) {
		const p = state.provinces[i];
		if (
			worldX < p.bbox.minX ||
			worldX > p.bbox.maxX ||
			worldY < p.bbox.minY ||
			worldY > p.bbox.maxY
		)
			continue;

		if (pointInPolygon({ x: worldX, y: worldY }, p.points)) {
			return p;
		}
	}
	return null;
}

export function findClosestSegment(worldX, worldY) {
	let minDistSq = Infinity;
	let insertTargets = [];
	const mousePt = { x: worldX, y: worldY };
	const thresholdSq = Math.pow(state.SEGMENT_HOVER_DIST / state.camera.zoom, 2);
	const hoverPad = Math.sqrt(thresholdSq);

	state.provinces.forEach((prov) => {
		if (
			worldX < prov.bbox.minX - hoverPad ||
			worldX > prov.bbox.maxX + hoverPad ||
			worldY < prov.bbox.minY - hoverPad ||
			worldY > prov.bbox.maxY + hoverPad
		)
			return;

		const pts = prov.points;
		for (let i = 0; i < pts.length; i++) {
			const p1 = pts[i];
			const p2 = pts[(i + 1) % pts.length];
			const dSq = distToSegmentSquared(mousePt, p1, p2);
			if (dSq < minDistSq) minDistSq = dSq;
		}
	});

	if (minDistSq > thresholdSq) return null;

	let projected = null;
	state.provinces.forEach((prov) => {
		if (
			worldX < prov.bbox.minX - hoverPad ||
			worldX > prov.bbox.maxX + hoverPad ||
			worldY < prov.bbox.minY - hoverPad ||
			worldY > prov.bbox.maxY + hoverPad
		)
			return;

		const pts = prov.points;
		for (let i = 0; i < pts.length; i++) {
			const p1 = pts[i];
			const p2 = pts[(i + 1) % pts.length];
			const dSq = distToSegmentSquared(mousePt, p1, p2);
			if (Math.abs(dSq - minDistSq) < 0.0001) {
				if (!projected) projected = getProjectedPoint(mousePt, p1, p2);
				insertTargets.push({ province: prov, index: i + 1 });
			}
		}
	});
	if (projected && insertTargets.length > 0)
		return { x: projected.x, y: projected.y, insertTargets };
	return null;
}

export function getProvincesInBox(x1, y1, x2, y2) {
	const left = Math.min(x1, x2);
	const right = Math.max(x1, x2);
	const top = Math.min(y1, y2);
	const bottom = Math.max(y1, y2);

	const result = [];
	state.provinces.forEach((p) => {
		if (
			p.bbox.maxX < left ||
			p.bbox.minX > right ||
			p.bbox.maxY < top ||
			p.bbox.minY > bottom
		)
			return;

		let isInside = false;
		for (const pt of p.points) {
			if (pt.x >= left && pt.x <= right && pt.y >= top && pt.y <= bottom) {
				isInside = true;
				break;
			}
		}
		if (isInside) result.push(p);
	});
	return result;
}

export function getRefImageAt(x, y) {
	for (let i = state.referenceImages.length - 1; i >= 0; i--) {
		const r = state.referenceImages[i];
		if (x >= r.x && x <= r.x + r.width && y >= r.y && y <= r.y + r.height) {
			return r;
		}
	}
	return null;
}

export function isOverRefHandle(x, y, ref) {
	const handleSize = 15 / state.camera.zoom;
	const hx = ref.x + ref.width;
	const hy = ref.y + ref.height;
	return Math.abs(x - hx) <= handleSize && Math.abs(y - hy) <= handleSize;
}
