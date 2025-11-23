export function clamp(val, min, max) {
	return Math.max(min, Math.min(max, val));
}

export function getDistance(p1, p2) {
	return Math.sqrt(Math.pow(p1.x - p2.x, 2) + Math.pow(p1.y - p2.y, 2));
}

export function pointInPolygon(point, vs) {
	let x = point.x,
		y = point.y;
	let inside = false;
	for (let i = 0, j = vs.length - 1; i < vs.length; j = i++) {
		let xi = vs[i].x,
			yi = vs[i].y;
		let xj = vs[j].x,
			yj = vs[j].y;
		let intersect =
			yi > y != yj > y && x < ((xj - xi) * (y - yi)) / (yj - yi) + xi;
		if (intersect) inside = !inside;
	}
	return inside;
}

export function distToSegmentSquared(p, v, w) {
	const l2 = Math.pow(v.x - w.x, 2) + Math.pow(v.y - w.y, 2);
	if (l2 === 0) return Math.pow(p.x - v.x, 2) + Math.pow(p.y - v.y, 2);
	let t = ((p.x - v.x) * (w.x - v.x) + (p.y - v.y) * (w.y - v.y)) / l2;
	t = Math.max(0, Math.min(1, t));
	return (
		Math.pow(p.x - (v.x + t * (w.x - v.x)), 2) +
		Math.pow(p.y - (v.y + t * (w.y - v.y)), 2)
	);
}

export function getProjectedPoint(p, v, w) {
	const l2 = Math.pow(v.x - w.x, 2) + Math.pow(v.y - w.y, 2);
	if (l2 === 0) return { x: v.x, y: v.y };
	let t = ((p.x - v.x) * (w.x - v.x) + (p.y - v.y) * (w.y - v.y)) / l2;
	t = Math.max(0, Math.min(1, t));
	return { x: v.x + t * (w.x - v.x), y: v.y + t * (w.y - v.y) };
}

export function areProvincesConnected(p1, p2) {
	// Fast fail: BBox check
	if (
		p1.bbox.maxX < p2.bbox.minX ||
		p1.bbox.minX > p2.bbox.maxX ||
		p1.bbox.maxY < p2.bbox.minY ||
		p1.bbox.minY > p2.bbox.maxY
	) {
		return false;
	}
	// Detailed check: Shared vertices
	// We assume exact coordinate match for shared vertices
	for (const pt1 of p1.points) {
		for (const pt2 of p2.points) {
			if (Math.abs(pt1.x - pt2.x) < 0.1 && Math.abs(pt1.y - pt2.y) < 0.1) {
				return true;
			}
		}
	}
	return false;
}
