import React, { useState, useRef, useEffect, useCallback } from "react";
import { ZoomIn, ZoomOut, Maximize, X } from "lucide-react";
import { STAR_TYPES } from "../utils/constants";

export default function GalaxyMapViewer({
	systems,
	connections,
	empires,
	onClose,
}) {
	// --- State ---
	const [view, setView] = useState({ x: 0, y: 0, zoom: 1 });
	const [isDraggingCanvas, setIsDraggingCanvas] = useState(false);
	const [dragStart, setDragStart] = useState({ x: 0, y: 0 });
	const [hoveredId, setHoveredId] = useState(null);

	// --- Refs ---
	const canvasRef = useRef(null);
	const territoryCanvasRef = useRef(null);

	// --- Helpers ---
	const screenToWorld = useCallback(
		(screenX, screenY) => {
			if (!canvasRef.current) return { x: 0, y: 0 };
			const rect = canvasRef.current.getBoundingClientRect();
			return {
				x: (screenX - rect.left - view.x) / view.zoom,
				y: (screenY - rect.top - view.y) / view.zoom,
			};
		},
		[view]
	);

	// --- Event Handlers ---
	const handleWheel = (e) => {
		if (e.ctrlKey || e.metaKey) e.preventDefault();
		const zoomSensitivity = 0.001;
		const newZoom = Math.max(
			0.1,
			Math.min(5, view.zoom - e.deltaY * zoomSensitivity)
		);
		const rect = canvasRef.current.getBoundingClientRect();
		const mouseX = e.clientX - rect.left;
		const mouseY = e.clientY - rect.top;
		const worldMouseX = (mouseX - view.x) / view.zoom;
		const worldMouseY = (mouseY - view.y) / view.zoom;
		setView({
			x: mouseX - worldMouseX * newZoom,
			y: mouseY - worldMouseY * newZoom,
			zoom: newZoom,
		});
	};

	const handleMouseDown = (e) => {
		if (e.button === 0 || e.button === 1 || e.button === 2) {
			setIsDraggingCanvas(true);
			setDragStart({ x: e.clientX - view.x, y: e.clientY - view.y });
		}
	};

	const handleMouseMove = (e) => {
		if (isDraggingCanvas) {
			setView({
				x: e.clientX - dragStart.x,
				y: e.clientY - dragStart.y,
				zoom: view.zoom,
			});
		}
	};

	const handleMouseUp = () => {
		setIsDraggingCanvas(false);
	};

	// --- Logic: Territory Renderer ---
	useEffect(() => {
		const canvas = territoryCanvasRef.current;
		if (!canvas) return;

		const ctx = canvas.getContext("2d");
		const { width, height } = canvas.getBoundingClientRect();

		const SCALE_FACTOR = 0.5;
		canvas.width = width * SCALE_FACTOR;
		canvas.height = height * SCALE_FACTOR;

		const w = canvas.width;
		const h = canvas.height;
		const pixelCount = w * h;

		ctx.clearRect(0, 0, w, h);

		const ownedSystems = systems.filter((s) => s.ownerId);
		if (ownedSystems.length === 0) return;

		const empireMeta = {};
		const empireIdToInt = {};
		const intToEmpireId = {};
		let empireCounter = 1;
		let maxInfluence = 0;

		empires.forEach((e) => {
			const r = parseInt(e.color.substr(1, 2), 16);
			const g = parseInt(e.color.substr(3, 2), 16);
			const b = parseInt(e.color.substr(5, 2), 16);
			const inf = e.influence || 50;
			empireMeta[e.id] = { r, g, b, influence: inf };
			empireIdToInt[e.id] = empireCounter;
			intToEmpireId[empireCounter] = e.id;
			empireCounter++;
			if (inf > maxInfluence) maxInfluence = inf;
		});

		const topLeft = { x: -view.x / view.zoom, y: -view.y / view.zoom };
		const bottomRight = {
			x: (width - view.x) / view.zoom,
			y: (height - view.y) / view.zoom,
		};
		const MAX_INFLUENCE_RADIUS = 30 + maxInfluence + 120; // Margin

		const visibleSystems = ownedSystems.filter(
			(s) =>
				s.x >= topLeft.x - MAX_INFLUENCE_RADIUS &&
				s.x <= bottomRight.x + MAX_INFLUENCE_RADIUS &&
				s.y >= topLeft.y - MAX_INFLUENCE_RADIUS &&
				s.y <= bottomRight.y + MAX_INFLUENCE_RADIUS
		);

		if (visibleSystems.length === 0) return;

		const maxStrengthBuffer = new Float32Array(pixelCount).fill(-9999);
		const runnerUpStrengthBuffer = new Float32Array(pixelCount).fill(-9999);
		const ownerBuffer = new Int32Array(pixelCount).fill(-1);

		visibleSystems.forEach((sys) => {
			const meta = empireMeta[sys.ownerId];
			if (!meta) return;
			const ownerInt = empireIdToInt[sys.ownerId];

			const canvasX = (sys.x * view.zoom + view.x) * SCALE_FACTOR;
			const canvasY = (sys.y * view.zoom + view.y) * SCALE_FACTOR;

			const radiusWorld = 30 + meta.influence + (sys.influence || 0);
			const radiusCanvas = radiusWorld * view.zoom * SCALE_FACTOR;
			const radiusSqCanvas = radiusCanvas * radiusCanvas;

			const minX = Math.max(0, Math.floor(canvasX - radiusCanvas));
			const maxX = Math.min(w - 1, Math.ceil(canvasX + radiusCanvas));
			const minY = Math.max(0, Math.floor(canvasY - radiusCanvas));
			const maxY = Math.min(h - 1, Math.ceil(canvasY + radiusCanvas));

			for (let y = minY; y <= maxY; y++) {
				const rowOffset = y * w;
				const dy = y - canvasY;
				const dySq = dy * dy;

				for (let x = minX; x <= maxX; x++) {
					const dx = x - canvasX;
					const distSq = dx * dx + dySq;

					if (distSq < radiusSqCanvas) {
						const dist = Math.sqrt(distSq);
						const distWorld = dist / (view.zoom * SCALE_FACTOR);
						const strength = radiusWorld - distWorld;

						const idx = rowOffset + x;
						const currentMax = maxStrengthBuffer[idx];

						if (strength > currentMax) {
							const currentOwnerInt = ownerBuffer[idx];
							if (currentOwnerInt !== -1 && currentOwnerInt !== ownerInt) {
								runnerUpStrengthBuffer[idx] = currentMax;
							}
							maxStrengthBuffer[idx] = strength;
							ownerBuffer[idx] = ownerInt;
						} else {
							const currentOwnerInt = ownerBuffer[idx];
							if (
								strength > runnerUpStrengthBuffer[idx] &&
								currentOwnerInt !== ownerInt
							) {
								runnerUpStrengthBuffer[idx] = strength;
							}
						}
					}
				}
			}
		});

		const imgData = ctx.createImageData(w, h);
		const data = imgData.data;
		const BORDER_THICKNESS = 2.0 / view.zoom;

		for (let i = 0; i < pixelCount; i++) {
			const maxStrength = maxStrengthBuffer[i];
			if (maxStrength <= 0) continue;

			const ownerInt = ownerBuffer[i];
			if (ownerInt === -1) continue;

			const empireId = intToEmpireId[ownerInt];
			const meta = empireMeta[empireId];
			const runnerUpStrength = runnerUpStrengthBuffer[i];
			const pixelIndex = i * 4;

			let alpha = 0.15;
			let isBorder = false;

			if (runnerUpStrength > -9000) {
				const delta = maxStrength - runnerUpStrength;
				if (delta < BORDER_THICKNESS) isBorder = true;
			}
			if (maxStrength < BORDER_THICKNESS) isBorder = true;

			if (isBorder) {
				alpha = 1.0;
			} else {
				const y = Math.floor(i / w);
				if (y % 2 === 0) alpha = 0.1;
				else alpha = 0.2;
			}

			data[pixelIndex] = meta.r;
			data[pixelIndex + 1] = meta.g;
			data[pixelIndex + 2] = meta.b;
			data[pixelIndex + 3] = Math.min(255, alpha * 255);
		}

		ctx.putImageData(imgData, 0, 0);
	}, [systems, empires, view]);

	// --- Logic: Labels ---
	const getEmpireLabels = (empireId, influence = 50) => {
		const ownedSystems = systems.filter((s) => s.ownerId === empireId);
		if (ownedSystems.length === 0) return [];

		const clusters = [];
		const visited = new Set();

		ownedSystems.forEach((sys) => {
			if (visited.has(sys.id)) return;
			const cluster = [];
			const queue = [sys];
			visited.add(sys.id);

			while (queue.length > 0) {
				const current = queue.pop();
				cluster.push(current);
				const currentRadius = 30 + influence + (current.influence || 0);

				ownedSystems.forEach((neighbor) => {
					if (!visited.has(neighbor.id)) {
						const dist = Math.sqrt(
							Math.pow(neighbor.x - current.x, 2) +
								Math.pow(neighbor.y - current.y, 2)
						);
						const neighborRadius = 30 + influence + (neighbor.influence || 0);
						const mergeThreshold = currentRadius + neighborRadius + 10;

						if (dist < mergeThreshold) {
							visited.add(neighbor.id);
							queue.push(neighbor);
						}
					}
				});
			}
			clusters.push(cluster);
		});

		return clusters.map((cluster) => {
			const sum = cluster.reduce(
				(acc, s) => ({ x: acc.x + s.x, y: acc.y + s.y }),
				{ x: 0, y: 0 }
			);
			return {
				key: cluster[0].id,
				x: sum.x / cluster.length,
				y: sum.y / cluster.length,
				count: cluster.length,
			};
		});
	};

	return (
		<div className="w-full h-screen bg-slate-950 overflow-hidden font-sans text-slate-200 flex flex-col relative">

			{/* --- Controls --- */}
			<div className="absolute top-4 right-4 z-50 flex gap-2">
				<div className="bg-slate-900/90 backdrop-blur-md p-2 rounded-2xl shadow-xl border border-slate-800 flex gap-2">
					<button
						onClick={() =>
							setView((v) => ({ ...v, zoom: Math.min(5, v.zoom * 1.2) }))
						}
						className="p-2 text-slate-400 hover:text-white rounded-lg hover:bg-slate-800"
					>
						<ZoomIn size={20} />
					</button>
					<button
						onClick={() =>
							setView((v) => ({ ...v, zoom: Math.max(0.1, v.zoom / 1.2) }))
						}
						className="p-2 text-slate-400 hover:text-white rounded-lg hover:bg-slate-800"
					>
						<ZoomOut size={20} />
					</button>
					<button
						onClick={() => setView({ x: 0, y: 0, zoom: 1 })}
						className="p-2 text-slate-400 hover:text-white rounded-lg hover:bg-slate-800"
					>
						<Maximize size={20} />
					</button>
				</div>
				{onClose && (
					<button
						onClick={onClose}
						className="bg-red-500/20 hover:bg-red-500/30 text-red-400 backdrop-blur-md p-3 rounded-2xl shadow-xl border border-red-500/30 transition-colors"
					>
						<X size={20} />
					</button>
				)}
			</div>

			{/* --- Canvas Area --- */}
			<div
				ref={canvasRef}
				className="flex-1 relative overflow-hidden cursor-grab active:cursor-grabbing"
				onMouseDown={handleMouseDown}
				onMouseMove={handleMouseMove}
				onMouseUp={handleMouseUp}
				onMouseLeave={handleMouseUp}
				onWheel={handleWheel}
			>
				{/* --- PARALLAX BACKGROUNDS --- */}
				<div className="absolute inset-0 bg-[#020617]" />
				<div
					className="absolute inset-0 opacity-40 pointer-events-none"
					style={{
						background:
							"radial-gradient(circle at 20% 30%, #1e1b4b 0%, transparent 40%), radial-gradient(circle at 80% 70%, #312e81 0%, transparent 40%)",
						transform: `translate(${view.x * 0.05}px, ${view.y * 0.05}px)`,
						width: "150%",
						height: "150%",
						left: "-25%",
						top: "-25%",
						transition: "transform 0.1s linear",
					}}
				/>
				<div
					className="absolute inset-0 opacity-60 pointer-events-none"
					style={{
						backgroundImage:
							"radial-gradient(1.5px 1.5px at 20px 30px, #cbd5e1, rgba(0,0,0,0)), radial-gradient(1.5px 1.5px at 40px 70px, #fff, rgba(0,0,0,0)), radial-gradient(2px 2px at 90px 40px, #94a3b8, rgba(0,0,0,0)), radial-gradient(1px 1px at 160px 120px, #cbd5e1, rgba(0,0,0,0))",
						backgroundSize: "300px 300px",
						transform: `translate(${view.x * 0.1}px, ${view.y * 0.1}px)`,
						width: "200%",
						height: "200%",
						left: "-50%",
						top: "-50%",
					}}
				/>
				<div
					className="absolute inset-0 opacity-10 pointer-events-none"
					style={{
						backgroundImage: `
              linear-gradient(to right, #334155 1px, transparent 1px),
              linear-gradient(to bottom, #334155 1px, transparent 1px)
            `,
						backgroundSize: `${50 * view.zoom}px ${50 * view.zoom}px`,
						backgroundPosition: `${view.x}px ${view.y}px`,
					}}
				/>

				{/* --- Territory Map --- */}
				<canvas
					ref={territoryCanvasRef}
					className="absolute inset-0 pointer-events-none"
					style={{
						width: "100%",
						height: "100%",
						imageRendering: "pixelated",
					}}
				/>

				{/* --- SVG Layer --- */}
				<svg className="w-full h-full pointer-events-none absolute inset-0">
					<g transform={`translate(${view.x}, ${view.y}) scale(${view.zoom})`}>
						{/* Connections */}
						{connections.map((conn, i) => {
							const start = systems.find((s) => s.id === conn.from);
							const end = systems.find((s) => s.id === conn.to);
							if (!start || !end) return null;
							return (
								<g key={i}>
									<line
										x1={start.x}
										y1={start.y}
										x2={end.x}
										y2={end.y}
										stroke="#6366f1"
										strokeWidth={6 / view.zoom}
										strokeOpacity={0.1}
										strokeLinecap="round"
									/>
									<line
										x1={start.x}
										y1={start.y}
										x2={end.x}
										y2={end.y}
										stroke="#818cf8"
										strokeWidth={3 / view.zoom}
										strokeOpacity={0.3}
										strokeLinecap="round"
									/>
									<line
										x1={start.x}
										y1={start.y}
										x2={end.x}
										y2={end.y}
										stroke="#e2e8f0"
										strokeWidth={1 / view.zoom}
										strokeOpacity={0.6}
										strokeLinecap="round"
									/>
								</g>
							);
						})}

						{/* Systems */}
						{systems.map((sys) => {
							const starData =
								STAR_TYPES.find((t) => t.id === sys.type) || STAR_TYPES[0];
							const baseRadius = 6;
							const systemOpacity = Math.min(
								1,
								Math.max(0.1, (view.zoom - 0.5) * 2.0)
							);
							const nameOpacity = Math.min(
								1,
								Math.max(0, (view.zoom - 0.7) * 2.0)
							);

							const seed = sys.id
								.split("")
								.reduce((acc, c) => acc + c.charCodeAt(0), 0);
							const animDelay = (seed % 2000) / -1000;
							const animDuration = 3 + (seed % 4000) / 1000;

							return (
								<g
									key={sys.id}
									transform={`translate(${sys.x}, ${sys.y})`}
									className="pointer-events-auto cursor-help"
									onMouseEnter={() => setHoveredId(sys.id)}
									onMouseLeave={() => setHoveredId(null)}
									style={{
										opacity: systemOpacity,
										transition: "opacity 0.2s",
									}}
								>
									{/* Hover Halo */}
									{hoveredId === sys.id && (
										<circle
											r={baseRadius * 3}
											fill="transparent"
											stroke="#475569"
											strokeWidth={1 / view.zoom}
											strokeOpacity={0.8}
										/>
									)}

									<circle
										r={baseRadius * 1.5}
										fill={starData.glow}
										className="blur-sm"
										style={{
											animation: `pulse-glow ${animDuration}s ease-in-out infinite`,
											animationDelay: `${animDelay}s`,
										}}
									/>
									<circle
										r={baseRadius}
										fill={starData.color}
										className="drop-shadow-md"
										style={{
											animation: `pulse-star ${animDuration}s ease-in-out infinite`,
											animationDelay: `${animDelay}s`,
										}}
									/>
									{sys.ownerId && (
										<circle
											r={baseRadius + 4}
											fill="transparent"
											stroke={
												empires.find((e) => e.id === sys.ownerId)?.color ||
												"#fff"
											}
											strokeWidth={2}
											opacity={0.8}
										/>
									)}
									<text
										y={-baseRadius - 10}
										textAnchor="middle"
										fill="#94a3b8"
										style={{
											fontSize: Math.max(10, 14 / view.zoom),
											textShadow: "0 2px 4px rgba(0,0,0,0.8)",
											pointerEvents: "none",
											userSelect: "none",
											opacity: nameOpacity,
										}}
									>
										{sys.name}
									</text>
								</g>
							);
						})}

						{/* Empire Labels */}
						{empires.map((empire) => {
							const labels = getEmpireLabels(empire.id, empire.influence || 50);
							return labels.map((label) => {
								const zoomFactor = Math.max(1, 0.6 / view.zoom);
								const baseSize = 10 + label.count * 2;
								const fontSize = Math.min(
									120,
									Math.max(16, baseSize * zoomFactor)
								);
								const labelOpacity = Math.max(
									0,
									Math.min(0.8, 1.5 - view.zoom)
								);

								if (labelOpacity <= 0.05) return null;

								return (
									<text
										key={`label-${empire.id}-${label.key}`}
										x={label.x}
										y={label.y}
										textAnchor="middle"
										fill={empire.color}
										opacity={labelOpacity}
										fontWeight="900"
										fontSize={fontSize}
										style={{
											textTransform: "uppercase",
											letterSpacing: "0.1em",
											pointerEvents: "none",
											textShadow: "0 4px 20px rgba(0,0,0,0.9)",
											dominantBaseline: "middle",
											transition:
												"font-size 0.2s ease-out, opacity 0.2s ease-out",
										}}
									>
										{empire.name}
									</text>
								);
							});
						})}
					</g>
				</svg>
			</div>
		</div>
	);
}
