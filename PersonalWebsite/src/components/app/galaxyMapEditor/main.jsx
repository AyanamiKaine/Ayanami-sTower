import React, { useState, useRef, useEffect, useCallback } from "react";
import {
    MousePointer2,
    Plus,
    Network,
    Hand,
    Download,
    Upload,
    ZoomIn,
    ZoomOut,
    Maximize,
    Sparkles,
    Shield,
    Paintbrush,
    Eye,
} from "lucide-react";

import {
    generateId,
    getDistance,
    generateName,
    generateEmpireName,
} from "./utils/helpers";
import { STAR_TYPES, EMPIRE_COLORS } from "./utils/constants";
import ToolbarButton from "./components/ToolbarButton";
import EmpireManager from "./components/EmpireManager";
import GalaxyGenerator from "./components/GalaxyGenerator";
import SystemInspector from "./components/SystemInspector";
import GalaxyMapViewer from "./components/GalaxyMapViewer";

export default function GalaxyForge() {
    // --- State ---

    // Data
    const [systems, setSystems] = useState([]);
    const [connections, setConnections] = useState([]);
    const [empires, setEmpires] = useState([]);

    // Viewport
    const [view, setView] = useState({ x: 0, y: 0, zoom: 1 });
    const [isDraggingCanvas, setIsDraggingCanvas] = useState(false);
    const [dragStart, setDragStart] = useState({ x: 0, y: 0 });

    // Interaction
    const [tool, setTool] = useState("select"); // select, add, link, pan, paint
    const [selectedIds, setSelectedIds] = useState(new Set());
    const [hoveredId, setHoveredId] = useState(null);
    const [linkStartId, setLinkStartId] = useState(null);
    const [isDraggingSystem, setIsDraggingSystem] = useState(false);
    const [selectionBox, setSelectionBox] = useState(null);

    // Empire State
    const [showEmpireManager, setShowEmpireManager] = useState(false);
    const [activeEmpireId, setActiveEmpireId] = useState(null); // The empire currently being painted

    // Generator State
    const [showGenerator, setShowGenerator] = useState(false);
    const [genConfig, setGenConfig] = useState({
        count: 50,
        radius: 500,
        type: "spiral",
        arms: 3,
        scatter: 0.3,
        connections: 2,
    });

    // Viewer State
    const [isPreviewMode, setIsPreviewMode] = useState(false);

    // UI
    const canvasRef = useRef(null);
    const territoryCanvasRef = useRef(null); // NEW: For drawing pixel territories
    const fileInputRef = useRef(null);
    const dragOrigin = useRef({ mouse: { x: 0, y: 0 }, systems: {} });
    const [mousePos, setMousePos] = useState({ x: 0, y: 0 });

    // --- Persistance ---

    useEffect(() => {
        const savedData = localStorage.getItem("galaxy-forge-data");
        if (savedData) {
            try {
                const parsed = JSON.parse(savedData);
                setSystems(parsed.systems || []);
                setConnections(parsed.connections || []);
                setEmpires(parsed.empires || []);
            } catch (e) {
                console.error("Failed to load saved data");
            }
        } else {
            // Initial Seed
            const initialSystems = [
                {
                    id: "sol",
                    x: 0,
                    y: 0,
                    name: "Sol",
                    type: "yellow",
                    ownerId: null,
                },
                {
                    id: "alpha",
                    x: 200,
                    y: -100,
                    name: "Alpha Centauri",
                    type: "red",
                    ownerId: null,
                },
                {
                    id: "sirius",
                    x: -150,
                    y: 150,
                    name: "Sirius",
                    type: "blue",
                    ownerId: null,
                },
            ];
            setSystems(initialSystems);
            setConnections([{ from: "sol", to: "alpha" }]);
        }
    }, []);

    useEffect(() => {
        localStorage.setItem(
            "galaxy-forge-data",
            JSON.stringify({ systems, connections, empires })
        );
    }, [systems, connections, empires]);

    // --- LOGIC: TERRITORY RENDERER (CANVAS) ---

    // This effect handles the complex "Pushing Borders" logic using a pixel field shader
    // OPTIMIZED: Uses a "Scatter" approach (System -> Pixels) instead of "Gather" (Pixel -> Systems)
    // This reduces complexity from O(Pixels * Systems) to O(Systems * Area + Pixels)
    useEffect(() => {
        const canvas = territoryCanvasRef.current;
        if (!canvas) return;

        const ctx = canvas.getContext("2d");
        const { width, height } = canvas.getBoundingClientRect();

        // Resolution Downscaling for Performance
        // Higher = Sharper but slower. 0.5 is a good balance.
        const SCALE_FACTOR = 0.5;
        canvas.width = width * SCALE_FACTOR;
        canvas.height = height * SCALE_FACTOR;

        const w = canvas.width;
        const h = canvas.height;
        const pixelCount = w * h;

        // Clear
        ctx.clearRect(0, 0, w, h);

        // Optimization: Only consider owned systems
        const ownedSystems = systems.filter((s) => s.ownerId);
        if (ownedSystems.length === 0) return;

        // Pre-calculate empire colors and influence
        const empireMeta = {};
        const empireIdToInt = {};
        const intToEmpireId = {};
        let empireCounter = 1;
        let maxInfluence = 0;

        empires.forEach((e) => {
            // Convert hex to rgb
            const r = parseInt(e.color.substr(1, 2), 16);
            const g = parseInt(e.color.substr(3, 2), 16);
            const b = parseInt(e.color.substr(5, 2), 16);
            const inf = e.influence || 50;
            empireMeta[e.id] = { r, g, b, influence: inf };

            // Map ID to Int for the buffer
            empireIdToInt[e.id] = empireCounter;
            intToEmpireId[empireCounter] = e.id;
            empireCounter++;

            if (inf > maxInfluence) maxInfluence = inf;
        });

        // Optimization: Filter systems to only those affecting the viewport
        // Calculate Viewport World Bounds
        const topLeft = { x: -view.x / view.zoom, y: -view.y / view.zoom };
        const bottomRight = {
            x: (width - view.x) / view.zoom,
            y: (height - view.y) / view.zoom,
        };
        const MAX_INFLUENCE_RADIUS = 30 + maxInfluence + 20; // Dynamic safety margin

        const visibleSystems = ownedSystems.filter(
            (s) =>
                s.x >= topLeft.x - MAX_INFLUENCE_RADIUS &&
                s.x <= bottomRight.x + MAX_INFLUENCE_RADIUS &&
                s.y >= topLeft.y - MAX_INFLUENCE_RADIUS &&
                s.y <= bottomRight.y + MAX_INFLUENCE_RADIUS
        );

        if (visibleSystems.length === 0) return;

        // BUFFERS
        // We use Float32Array for speed.
        // Initialize with a value that represents "no strength"
        const maxStrengthBuffer = new Float32Array(pixelCount).fill(-9999);
        const runnerUpStrengthBuffer = new Float32Array(pixelCount).fill(-9999);
        const ownerBuffer = new Int32Array(pixelCount).fill(-1);

        // SCATTER PASS: Iterate systems and write to buffers
        visibleSystems.forEach((sys) => {
            const meta = empireMeta[sys.ownerId];
            if (!meta) return;
            const ownerInt = empireIdToInt[sys.ownerId];

            // Calculate System Position in Canvas Coordinates
            const canvasX = (sys.x * view.zoom + view.x) * SCALE_FACTOR;
            const canvasY = (sys.y * view.zoom + view.y) * SCALE_FACTOR;

            // Calculate Radius in Canvas Pixels
            // INFLUENCE LOGIC: Base (30) + Empire Global + System Local
            const radiusWorld = 30 + meta.influence + (sys.influence || 0);
            const radiusCanvas = radiusWorld * view.zoom * SCALE_FACTOR;
            const radiusSqCanvas = radiusCanvas * radiusCanvas;

            // Bounding Box on Canvas (Clamped)
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
                        // Convert back to World Units for strength calculation to match border logic
                        const distWorld = dist / (view.zoom * SCALE_FACTOR);
                        const strength = radiusWorld - distWorld;

                        const idx = rowOffset + x;
                        const currentMax = maxStrengthBuffer[idx];

                        if (strength > currentMax) {
                            // Demote current best to runner up (if different empire)
                            const currentOwnerInt = ownerBuffer[idx];
                            if (
                                currentOwnerInt !== -1 &&
                                currentOwnerInt !== ownerInt
                            ) {
                                runnerUpStrengthBuffer[idx] = currentMax;
                            }

                            maxStrengthBuffer[idx] = strength;
                            ownerBuffer[idx] = ownerInt;
                        } else {
                            // Check if it beats runner up
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

        // Create ImageData
        const imgData = ctx.createImageData(w, h);
        const data = imgData.data;

        // Thresholds for "Crisp" Lines
        const BORDER_THICKNESS = 2.0 / view.zoom;

        // GATHER PASS: Iterate buffers and write colors
        for (let i = 0; i < pixelCount; i++) {
            const maxStrength = maxStrengthBuffer[i];

            // Optimization: Skip empty pixels
            if (maxStrength <= 0) continue;

            const ownerInt = ownerBuffer[i];
            if (ownerInt === -1) continue;

            const empireId = intToEmpireId[ownerInt];
            const meta = empireMeta[empireId];
            const runnerUpStrength = runnerUpStrengthBuffer[i];

            const pixelIndex = i * 4;

            let alpha = 0.15; // Base fill opacity
            let isBorder = false;

            // 1. Detect Internal Border (Conflict line between two empires)
            if (runnerUpStrength > -9000) {
                const delta = maxStrength - runnerUpStrength;
                if (delta < BORDER_THICKNESS) {
                    isBorder = true;
                }
            }

            // 2. Detect Outer Border (Edge of influence)
            if (maxStrength < BORDER_THICKNESS) {
                isBorder = true;
            }

            if (isBorder) {
                alpha = 1.0; // Solid Line
            } else {
                // SCANLINE EFFECT
                // Use Y coordinate from index
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
    }, [systems, empires, view, genConfig]); // Re-run when map or view changes

    // --- Logic: Empires ---

    const createEmpire = () => {
        const newEmpire = {
            id: generateId(),
            name: generateEmpireName(),
            color: EMPIRE_COLORS[empires.length % EMPIRE_COLORS.length],
            influence: 50, // Default influence value
        };
        setEmpires([...empires, newEmpire]);
        setActiveEmpireId(newEmpire.id);
        setTool("paint");
    };

    const deleteEmpire = (id) => {
        // Reset ownership of stars belonging to this empire
        setSystems((prev) =>
            prev.map((s) => (s.ownerId === id ? { ...s, ownerId: null } : s))
        );
        setEmpires((prev) => prev.filter((e) => e.id !== id));
        if (activeEmpireId === id) setActiveEmpireId(null);
    };

    const updateEmpire = (id, updates) => {
        setEmpires((prev) =>
            prev.map((e) => (e.id === id ? { ...e, ...updates } : e))
        );
    };

    // ISLAND DETECTION ALGORITHM
    // Returns an array of { x, y, count } objects, one for each disjoint cluster of stars
    const getEmpireLabels = (empireId, influence = 50) => {
        const ownedSystems = systems.filter((s) => s.ownerId === empireId);
        if (ownedSystems.length === 0) return [];

        const clusters = [];
        const visited = new Set();

        ownedSystems.forEach((sys) => {
            if (visited.has(sys.id)) return;

            // Start a new cluster (BFS Flood Fill)
            const cluster = [];
            const queue = [sys];
            visited.add(sys.id);

            while (queue.length > 0) {
                const current = queue.pop();
                cluster.push(current);

                // Calculate dynamic radius for the current system
                const currentRadius = 30 + influence + (current.influence || 0);

                ownedSystems.forEach((neighbor) => {
                    if (!visited.has(neighbor.id)) {
                        const dist = Math.sqrt(
                            Math.pow(neighbor.x - current.x, 2) +
                                Math.pow(neighbor.y - current.y, 2)
                        );

                        // Calculate dynamic radius for the neighbor
                        const neighborRadius =
                            30 + influence + (neighbor.influence || 0);

                        // Check if their influence zones overlap
                        // Overlap happens if distance < radius1 + radius2
                        // We add a small margin (+10) to ensure they visually merge
                        const mergeThreshold =
                            currentRadius + neighborRadius + 10;

                        if (dist < mergeThreshold) {
                            visited.add(neighbor.id);
                            queue.push(neighbor);
                        }
                    }
                });
            }
            clusters.push(cluster);
        });

        // Calculate center of mass for each cluster
        return clusters.map((cluster) => {
            const sum = cluster.reduce(
                (acc, s) => ({ x: acc.x + s.x, y: acc.y + s.y }),
                { x: 0, y: 0 }
            );
            return {
                key: cluster[0].id, // Stable key for React
                x: sum.x / cluster.length,
                y: sum.y / cluster.length,
                count: cluster.length,
            };
        });
    };

    // --- Logic: Generator ---

    const generateGalaxy = () => {
        const newSystems = [];
        const count = genConfig.count;
        const radius = genConfig.radius;
        const MIN_DIST = 30;

        let attempts = 0;
        const MAX_ATTEMPTS = count * 50;

        while (newSystems.length < count && attempts < MAX_ATTEMPTS) {
            attempts++;
            let x, y;
            let type =
                STAR_TYPES[Math.floor(Math.random() * STAR_TYPES.length)].id;

            if (genConfig.type === "spiral") {
                const i = newSystems.length;
                const armIndex = i % genConfig.arms;
                const armAngle = (armIndex / genConfig.arms) * 2 * Math.PI;
                const distance = Math.random() * radius;
                const spin = distance * 0.01;
                const randomAngle =
                    (Math.random() - 0.5) * genConfig.scatter * 2;
                const randomDist =
                    (Math.random() - 0.5) * (radius * genConfig.scatter * 0.5);
                const angle = armAngle + spin + randomAngle;
                const dist = distance + randomDist;
                x = Math.cos(angle) * dist;
                y = Math.sin(angle) * dist;
            } else if (genConfig.type === "ring") {
                const angle = Math.random() * 2 * Math.PI;
                const dist = radius * 0.8 + Math.random() * radius * 0.2;
                x = Math.cos(angle) * dist;
                y = Math.sin(angle) * dist;
            } else {
                const u1 = Math.random();
                const u2 = Math.random();
                const z =
                    Math.sqrt(-2.0 * Math.log(u1)) *
                    Math.cos(2.0 * Math.PI * u2);
                const dist = Math.abs(z) * (radius * 0.4);
                const angle = Math.random() * 2 * Math.PI;
                x = Math.cos(angle) * dist;
                y = Math.sin(angle) * dist;
            }

            const tooClose = newSystems.some((sys) => {
                const dx = sys.x - x;
                const dy = sys.y - y;
                return dx * dx + dy * dy < MIN_DIST * MIN_DIST;
            });

            if (!tooClose) {
                newSystems.push({
                    id: generateId(),
                    x,
                    y,
                    name: generateName(),
                    type,
                    ownerId: null,
                });
            }
        }

        const newConnections = [];
        const maxDist = radius * 0.4;

        if (genConfig.connections > 0) {
            newSystems.forEach((sys) => {
                const others = newSystems
                    .filter((s) => s.id !== sys.id)
                    .map((s) => ({ id: s.id, dist: getDistance(sys, s) }))
                    .filter((item) => item.dist < maxDist)
                    .sort((a, b) => a.dist - b.dist)
                    .slice(0, genConfig.connections);

                others.forEach((other) => {
                    const exists = newConnections.find(
                        (c) =>
                            (c.from === sys.id && c.to === other.id) ||
                            (c.from === other.id && c.to === sys.id)
                    );
                    if (!exists && Math.random() > 0.2) {
                        newConnections.push({ from: sys.id, to: other.id });
                    }
                });
            });
        }

        setSelectedIds(new Set());
        setLinkStartId(null);
        setHoveredId(null);
        setSystems(newSystems);
        setConnections(newConnections);
        setEmpires([]); // Clear empires on regen? Or keep them? Let's clear for fresh start.
        setShowGenerator(false);
        setView({ x: 0, y: 0, zoom: 0.6 });
    };

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
        if (e.button === 1 || tool === "pan" || e.getModifierState("Space")) {
            setIsDraggingCanvas(true);
            setDragStart({ x: e.clientX - view.x, y: e.clientY - view.y });
            return;
        }
        const worldPos = screenToWorld(e.clientX, e.clientY);

        if (tool === "add") {
            const newSystem = {
                id: generateId(),
                x: worldPos.x,
                y: worldPos.y,
                name: generateName(),
                type: STAR_TYPES[Math.floor(Math.random() * STAR_TYPES.length)]
                    .id,
                ownerId: null,
            };
            setSystems([...systems, newSystem]);
            setSelectedIds(new Set([newSystem.id]));
            setTool("select");
        } else if (tool === "paint") {
            // Paint Mode Logic
            if (hoveredId && activeEmpireId) {
                // Assign or Toggle? Assign seems better for painting.
                const currentSys = systems.find((s) => s.id === hoveredId);
                if (currentSys) {
                    // If clicking same empire, remove ownership (toggle off)
                    const newOwner =
                        currentSys.ownerId === activeEmpireId
                            ? null
                            : activeEmpireId;
                    updateSystem(hoveredId, { ownerId: newOwner });
                }
            }
        } else if (tool === "select") {
            if (hoveredId) {
                let newSelection = new Set(selectedIds);
                if (e.shiftKey) {
                    if (newSelection.has(hoveredId))
                        newSelection.delete(hoveredId);
                    else newSelection.add(hoveredId);
                    setSelectedIds(newSelection);
                } else {
                    if (!newSelection.has(hoveredId)) {
                        newSelection = new Set([hoveredId]);
                        setSelectedIds(newSelection);
                    }
                }
                setIsDraggingSystem(true);
                const initialPositions = {};
                systems.forEach((s) => {
                    if (newSelection.has(s.id))
                        initialPositions[s.id] = { x: s.x, y: s.y };
                });
                dragOrigin.current = {
                    mouse: worldPos,
                    systems: initialPositions,
                };
            } else {
                if (!e.shiftKey) setSelectedIds(new Set());
                setSelectionBox({ start: worldPos, current: worldPos });
            }
        } else if (tool === "link") {
            if (hoveredId) {
                if (linkStartId === null) {
                    setLinkStartId(hoveredId);
                } else {
                    if (linkStartId !== hoveredId) {
                        const exists = connections.find(
                            (c) =>
                                (c.from === linkStartId &&
                                    c.to === hoveredId) ||
                                (c.from === hoveredId && c.to === linkStartId)
                        );
                        if (!exists)
                            setConnections([
                                ...connections,
                                { from: linkStartId, to: hoveredId },
                            ]);
                    }
                    setLinkStartId(null);
                }
            } else {
                setLinkStartId(null);
            }
        }
    };

    const handleMouseMove = (e) => {
        const worldPos = screenToWorld(e.clientX, e.clientY);
        if (!isDraggingSystem) setMousePos(worldPos);

        if (isDraggingCanvas) {
            setView({
                x: e.clientX - dragStart.x,
                y: e.clientY - dragStart.y,
                zoom: view.zoom,
            });
            return;
        }

        // Dragging logic (move stars)
        if (isDraggingSystem && tool === "select") {
            const dx = worldPos.x - dragOrigin.current.mouse.x;
            const dy = worldPos.y - dragOrigin.current.mouse.y;
            setSystems((prev) =>
                prev.map((sys) => {
                    const startPos = dragOrigin.current.systems[sys.id];
                    return startPos
                        ? { ...sys, x: startPos.x + dx, y: startPos.y + dy }
                        : sys;
                })
            );
        }
        if (selectionBox)
            setSelectionBox((prev) => ({ ...prev, current: worldPos }));

        // Paint while dragging (brush behavior)
        if (
            tool === "paint" &&
            e.buttons === 1 &&
            hoveredId &&
            activeEmpireId
        ) {
            // Throttle slightly via React render cycle naturally
            const currentSys = systems.find((s) => s.id === hoveredId);
            if (currentSys && currentSys.ownerId !== activeEmpireId) {
                updateSystem(hoveredId, { ownerId: activeEmpireId });
            }
        }
    };

    const handleMouseUp = () => {
        if (selectionBox) {
            const x1 = Math.min(selectionBox.start.x, selectionBox.current.x);
            const x2 = Math.max(selectionBox.start.x, selectionBox.current.x);
            const y1 = Math.min(selectionBox.start.y, selectionBox.current.y);
            const y2 = Math.max(selectionBox.start.y, selectionBox.current.y);
            const newSelection = new Set(selectedIds);
            systems.forEach((sys) => {
                if (sys.x >= x1 && sys.x <= x2 && sys.y >= y1 && sys.y <= y2)
                    newSelection.add(sys.id);
            });
            setSelectedIds(newSelection);
            setSelectionBox(null);
        }
        setIsDraggingCanvas(false);
        setIsDraggingSystem(false);
    };

    const handleKeyDown = (e) => {
        if (isPreviewMode) return;
        if (e.key === "Delete" || e.key === "Backspace") {
            if (
                selectedIds.size > 0 &&
                document.activeElement.tagName !== "INPUT"
            )
                deleteSelected();
        }
        if (e.key === "Escape") {
            setLinkStartId(null);
            setSelectedIds(new Set());
            setShowGenerator(false);
            setShowEmpireManager(false);
        }
        if (document.activeElement.tagName !== "INPUT") {
            if (e.key === "v") setTool("select");
            if (e.key === "a") setTool("add");
            if (e.key === "l") setTool("link");
            if (e.key === "h") setTool("pan");
            if (e.key === "p") {
                setTool("paint");
                setShowEmpireManager(true);
            }
        }
    };

    // --- Logic ---

    const deleteSelected = () => {
        setSystems(systems.filter((s) => !selectedIds.has(s.id)));
        setConnections(
            connections.filter(
                (c) => !selectedIds.has(c.from) && !selectedIds.has(c.to)
            )
        );
        setSelectedIds(new Set());
    };

    const updateSystem = (id, updates) => {
        setSystems((prev) =>
            prev.map((s) => (s.id === id ? { ...s, ...updates } : s))
        );
    };

    const handleBulkUpdateEmpire = (newVal) => {
        setSystems((prev) =>
            prev.map((s) =>
                selectedIds.has(s.id) ? { ...s, ownerId: newVal } : s
            )
        );
    };

    const exportData = () => {
        const data = {
            version: "1.1",
            timestamp: new Date().toISOString(),
            systems,
            connections,
            empires,
        };
        const dataStr =
            "data:text/json;charset=utf-8," +
            encodeURIComponent(JSON.stringify(data, null, 2));
        const downloadAnchorNode = document.createElement("a");
        downloadAnchorNode.setAttribute("href", dataStr);
        downloadAnchorNode.setAttribute(
            "download",
            `galaxy_map_${Date.now()}.json`
        );
        document.body.appendChild(downloadAnchorNode);
        downloadAnchorNode.click();
        downloadAnchorNode.remove();
    };

    const importData = (e) => {
        const file = e.target.files[0];
        if (!file) return;
        const reader = new FileReader();
        reader.onload = (event) => {
            try {
                const parsed = JSON.parse(event.target.result);
                if (
                    Array.isArray(parsed.systems) &&
                    Array.isArray(parsed.connections)
                ) {
                    setSelectedIds(new Set());
                    setHoveredId(null);
                    setLinkStartId(null);
                    setSelectionBox(null);
                    setSystems(parsed.systems);
                    setConnections(parsed.connections);
                    setEmpires(parsed.empires || []);
                } else {
                    alert("Invalid map format.");
                }
            } catch (err) {
                alert("Failed to parse JSON.");
            }
        };
        reader.readAsText(file);
        e.target.value = null;
    };

    useEffect(() => {
        window.addEventListener("keydown", handleKeyDown);
        return () => window.removeEventListener("keydown", handleKeyDown);
    }, [selectedIds, systems, connections, showGenerator, activeEmpireId]);

    // --- Rendering ---

    const singleSelectedSystem =
        selectedIds.size === 1
            ? systems.find((s) => selectedIds.has(s.id))
            : null;

    if (isPreviewMode) {
        return (
            <GalaxyMapViewer
                systems={systems}
                connections={connections}
                empires={empires}
                onClose={() => setIsPreviewMode(false)}
            />
        );
    }

    return (
        <div className="w-full h-screen bg-slate-950 overflow-hidden font-sans text-slate-200 flex flex-col md:flex-row">
            {/* --- SVG Filter Definitions (Still useful for the canvas blur if needed, or UI elements) --- */}
            <svg style={{ position: "absolute", width: 0, height: 0 }}>
                <defs>
                    <filter id="gooey-empire">
                        <feGaussianBlur
                            in="SourceGraphic"
                            stdDeviation="15"
                            result="blur"
                        />
                        <feColorMatrix
                            in="blur"
                            mode="matrix"
                            values="1 0 0 0 0  0 1 0 0 0  0 0 1 0 0  0 0 0 19 -9"
                            result="goo"
                        />
                        <feComposite
                            in="SourceGraphic"
                            in2="goo"
                            operator="atop"
                        />
                    </filter>
                </defs>
            </svg>

            {/* --- Sidebar / Toolbar --- */}
            <div className="absolute top-4 left-4 z-20 flex flex-col gap-4 pointer-events-auto">
                <div className="bg-slate-900/90 backdrop-blur-md p-2 rounded-2xl shadow-xl border border-slate-800 flex flex-col gap-2">
                    <ToolbarButton
                        active={showGenerator}
                        onClick={() => setShowGenerator(!showGenerator)}
                        icon={Sparkles}
                        label="Galaxy Generator"
                    />

                    <ToolbarButton
                        active={showEmpireManager}
                        onClick={() => setShowEmpireManager(!showEmpireManager)}
                        icon={Shield}
                        label="Empire Manager"
                    />

                    <ToolbarButton
                        active={false}
                        onClick={() => setIsPreviewMode(true)}
                        icon={Eye}
                        label="Preview Mode"
                    />

                    <div className="h-px bg-slate-700 my-1"></div>

                    <ToolbarButton
                        active={tool === "select"}
                        onClick={() => setTool("select")}
                        icon={MousePointer2}
                        label="Select / Move"
                        shortcut="V"
                    />
                    <ToolbarButton
                        active={tool === "paint"}
                        onClick={() => {
                            setTool("paint");
                            setShowEmpireManager(true);
                        }}
                        icon={Paintbrush}
                        label="Empire Painter"
                        shortcut="P"
                    />
                    <ToolbarButton
                        active={tool === "add"}
                        onClick={() => setTool("add")}
                        icon={Plus}
                        label="Add System"
                        shortcut="A"
                    />
                    <ToolbarButton
                        active={tool === "link"}
                        onClick={() => setTool("link")}
                        icon={Network}
                        label="Hyperlane"
                        shortcut="L"
                    />
                    <ToolbarButton
                        active={tool === "pan"}
                        onClick={() => setTool("pan")}
                        icon={Hand}
                        label="Pan"
                        shortcut="H"
                    />

                    <div className="h-px bg-slate-700 my-1"></div>

                    <ToolbarButton
                        active={false}
                        onClick={exportData}
                        icon={Download}
                        label="Export Map"
                    />
                    <ToolbarButton
                        active={false}
                        onClick={() => fileInputRef.current.click()}
                        icon={Upload}
                        label="Import Map"
                    />
                    <input
                        type="file"
                        ref={fileInputRef}
                        className="hidden"
                        onChange={importData}
                        accept=".json"
                    />
                </div>

                <div className="bg-slate-900/90 backdrop-blur-md p-2 rounded-2xl shadow-xl border border-slate-800 flex flex-col gap-2">
                    <button
                        onClick={() =>
                            setView((v) => ({
                                ...v,
                                zoom: Math.min(5, v.zoom * 1.2),
                            }))
                        }
                        className="p-2 text-slate-400 hover:text-white rounded-lg hover:bg-slate-800"
                    >
                        <ZoomIn size={20} />
                    </button>
                    <div className="text-center text-xs text-slate-500 font-mono">
                        {Math.round(view.zoom * 100)}%
                    </div>
                    <button
                        onClick={() =>
                            setView((v) => ({
                                ...v,
                                zoom: Math.max(0.1, v.zoom / 1.2),
                            }))
                        }
                        className="p-2 text-slate-400 hover:text-white rounded-lg hover:bg-slate-800"
                    >
                        <ZoomOut size={20} />
                    </button>
                    <button
                        onClick={() => setView({ x: 0, y: 0, zoom: 1 })}
                        className="p-2 text-slate-400 hover:text-white rounded-lg hover:bg-slate-800"
                        title="Reset View"
                    >
                        <Maximize size={20} />
                    </button>
                </div>
            </div>

            {/* --- Empire Manager Modal --- */}
            {showEmpireManager && (
                <EmpireManager
                    empires={empires}
                    activeEmpireId={activeEmpireId}
                    tool={tool}
                    onClose={() => setShowEmpireManager(false)}
                    onCreateEmpire={createEmpire}
                    onDeleteEmpire={deleteEmpire}
                    onUpdateEmpire={updateEmpire}
                    onSetActiveEmpire={setActiveEmpireId}
                    onSetTool={setTool}
                />
            )}

            {/* --- Generator Modal --- */}
            {showGenerator && (
                <GalaxyGenerator
                    config={genConfig}
                    onConfigChange={setGenConfig}
                    onGenerate={generateGalaxy}
                    onClose={() => setShowGenerator(false)}
                />
            )}

            {/* --- Canvas Area --- */}
            <div
                ref={canvasRef}
                className={`flex-1 relative overflow-hidden cursor-${
                    tool === "pan" || isDraggingCanvas
                        ? "grabbing"
                        : tool === "paint"
                        ? "crosshair"
                        : tool === "select"
                        ? "default"
                        : "crosshair"
                }`}
                onMouseDown={handleMouseDown}
                onMouseMove={handleMouseMove}
                onMouseUp={handleMouseUp}
                onMouseLeave={handleMouseUp}
                onWheel={handleWheel}
            >
                {/* --- PARALLAX BACKGROUNDS --- */}

                {/* Layer 0: Deep Space Base */}
                <div className="absolute inset-0 bg-[#020617]" />

                {/* Layer 1: Distant Nebulae (Static/Slow) */}
                <div
                    className="absolute inset-0 opacity-40 pointer-events-none"
                    style={{
                        background:
                            "radial-gradient(circle at 20% 30%, #1e1b4b 0%, transparent 40%), radial-gradient(circle at 80% 70%, #312e81 0%, transparent 40%)",
                        transform: `translate(${view.x * 0.05}px, ${
                            view.y * 0.05
                        }px)`,
                        width: "150%",
                        height: "150%",
                        left: "-25%",
                        top: "-25%",
                        transition: "transform 0.1s linear",
                    }}
                />

                {/* Layer 2: Starfield (Repeating Pattern) */}
                <div
                    className="absolute inset-0 opacity-60 pointer-events-none"
                    style={{
                        backgroundImage:
                            "radial-gradient(1.5px 1.5px at 20px 30px, #cbd5e1, rgba(0,0,0,0)), radial-gradient(1.5px 1.5px at 40px 70px, #fff, rgba(0,0,0,0)), radial-gradient(2px 2px at 90px 40px, #94a3b8, rgba(0,0,0,0)), radial-gradient(1px 1px at 160px 120px, #cbd5e1, rgba(0,0,0,0))",
                        backgroundSize: "300px 300px",
                        transform: `translate(${view.x * 0.1}px, ${
                            view.y * 0.1
                        }px)`,
                        width: "200%",
                        height: "200%",
                        left: "-50%",
                        top: "-50%",
                    }}
                />

                {/* Background Grid (Fainter now) */}
                <div
                    className="absolute inset-0 opacity-10 pointer-events-none"
                    style={{
                        backgroundImage: `
              linear-gradient(to right, #334155 1px, transparent 1px),
              linear-gradient(to bottom, #334155 1px, transparent 1px)
            `,
                        backgroundSize: `${50 * view.zoom}px ${
                            50 * view.zoom
                        }px`,
                        backgroundPosition: `${view.x}px ${view.y}px`,
                    }}
                />

                {/* --- Layer 1: Territory Map (Canvas) --- */}
                {/* Removed filter: blur to make lines crisp */}
                <canvas
                    ref={territoryCanvasRef}
                    className="absolute inset-0 pointer-events-none"
                    style={{
                        width: "100%",
                        height: "100%",
                        imageRendering: "pixelated",
                    }}
                />

                {/* The Map SVG */}
                <svg className="w-full h-full pointer-events-none absolute inset-0">
                    <g
                        transform={`translate(${view.x}, ${view.y}) scale(${view.zoom})`}
                    >
                        {/* --- Layer 2: Connections (Moved up) --- */}
                        {connections.map((conn, i) => {
                            const start = systems.find(
                                (s) => s.id === conn.from
                            );
                            const end = systems.find((s) => s.id === conn.to);
                            if (!start || !end) return null;
                            return (
                                <g key={i}>
                                    {/* Outer Glow (Subtle) */}
                                    <line
                                        x1={start.x}
                                        y1={start.y}
                                        x2={end.x}
                                        y2={end.y}
                                        stroke="#6366f1" // Indigo-500
                                        strokeWidth={6 / view.zoom}
                                        strokeOpacity={0.1}
                                        strokeLinecap="round"
                                    />
                                    {/* Inner Glow (Brighter) */}
                                    <line
                                        x1={start.x}
                                        y1={start.y}
                                        x2={end.x}
                                        y2={end.y}
                                        stroke="#818cf8" // Indigo-400
                                        strokeWidth={3 / view.zoom}
                                        strokeOpacity={0.3}
                                        strokeLinecap="round"
                                    />
                                    {/* Core Line */}
                                    <line
                                        x1={start.x}
                                        y1={start.y}
                                        x2={end.x}
                                        y2={end.y}
                                        stroke="#e2e8f0" // Slate-200
                                        strokeWidth={1 / view.zoom}
                                        strokeOpacity={0.6}
                                        strokeLinecap="round"
                                    />
                                </g>
                            );
                        })}

                        {/* Ghost Connection Line */}
                        {tool === "link" && linkStartId && (
                            <line
                                x1={
                                    systems.find((s) => s.id === linkStartId)
                                        ?.x || 0
                                }
                                y1={
                                    systems.find((s) => s.id === linkStartId)
                                        ?.y || 0
                                }
                                x2={mousePos.x}
                                y2={mousePos.y}
                                stroke="#6366f1"
                                strokeWidth={2 / view.zoom}
                                strokeDasharray="5,5"
                            />
                        )}

                        {/* --- Layer 3: Systems (Moved up) --- */}
                        {systems.map((sys) => {
                            const isSelected = selectedIds.has(sys.id);
                            const isHovered = hoveredId === sys.id;
                            const isLinkStart = linkStartId === sys.id;
                            const starData =
                                STAR_TYPES.find((t) => t.id === sys.type) ||
                                STAR_TYPES[0];
                            const baseRadius = 5;

                            // Visual feedback for Paint Mode
                            const isOwnedByActive =
                                activeEmpireId &&
                                sys.ownerId === activeEmpireId;

                            // DYNAMIC OPACITY CALCULATIONS
                            // 1. System Body: Starts fading out below zoom 1.0, becomes very faint (0.1) by zoom 0.5
                            const systemOpacity = Math.min(
                                1,
                                Math.max(0.1, (view.zoom - 0.5) * 2.0)
                            );

                            // 2. System Name: Fades out faster (below zoom 1.2) and disappears completely below 0.7
                            //    This declutters the map to show the Empire Names clearly.
                            const nameOpacity = Math.min(
                                1,
                                Math.max(0, (view.zoom - 0.7) * 2.0)
                            );

                            return (
                                <g
                                    key={sys.id}
                                    transform={`translate(${sys.x}, ${sys.y})`}
                                    className="pointer-events-auto"
                                    onMouseEnter={() => setHoveredId(sys.id)}
                                    onMouseLeave={() => setHoveredId(null)}
                                    style={{
                                        opacity: isSelected ? 1 : systemOpacity,
                                        transition: "opacity 0.2s",
                                    }}
                                >
                                    {/* Paint Mode Feedback Halo */}
                                    {tool === "paint" &&
                                        activeEmpireId &&
                                        isHovered && (
                                            <circle
                                                r={20}
                                                fill="transparent"
                                                stroke="white"
                                                strokeOpacity={0.5}
                                                strokeDasharray="2,2"
                                            />
                                        )}

                                    {/* Selection/Hover Halo */}
                                    <circle
                                        r={baseRadius * 2.5}
                                        fill="transparent"
                                        stroke={
                                            isSelected
                                                ? "#818cf8"
                                                : isLinkStart
                                                ? "#34d399"
                                                : isHovered
                                                ? "#475569"
                                                : "transparent"
                                        }
                                        strokeWidth={1.5 / view.zoom}
                                        strokeOpacity={0.8}
                                        strokeDasharray={
                                            isSelected ? "4,2" : "0"
                                        }
                                    />

                                    {/* Star Glow */}
                                    <circle
                                        r={baseRadius * 1.5}
                                        fill={starData.glow}
                                        opacity={0.2}
                                        className="blur-sm"
                                    />

                                    {/* Star Body */}
                                    <circle
                                        r={baseRadius}
                                        fill={starData.color}
                                        className="drop-shadow-md cursor-pointer"
                                    />

                                    {/* Empire Ownership Indicator (Small ring around star) */}
                                    {sys.ownerId && (
                                        <circle
                                            r={baseRadius + 4}
                                            fill="transparent"
                                            stroke={
                                                empires.find(
                                                    (e) => e.id === sys.ownerId
                                                )?.color || "#fff"
                                            }
                                            strokeWidth={2}
                                            opacity={0.8}
                                        />
                                    )}

                                    {/* Label */}
                                    <text
                                        y={-baseRadius - 10}
                                        textAnchor="middle"
                                        fill={
                                            isSelected ? "#ffffff" : "#94a3b8"
                                        }
                                        fontWeight={
                                            isSelected ? "bold" : "normal"
                                        }
                                        style={{
                                            fontSize: Math.max(
                                                10,
                                                14 / view.zoom
                                            ),
                                            textShadow:
                                                "0 2px 4px rgba(0,0,0,0.8)",
                                            pointerEvents: "none",
                                            userSelect: "none",
                                            opacity: isSelected
                                                ? 1
                                                : nameOpacity, // Apply aggressive fade to text
                                        }}
                                    >
                                        {sys.name}
                                    </text>
                                </g>
                            );
                        })}

                        {/* --- Layer 4: Empire Labels (Moved to Bottom/Top) --- */}
                        {empires.map((empire) => {
                            const labels = getEmpireLabels(
                                empire.id,
                                empire.influence || 50
                            );

                            return labels.map((label) => {
                                // STRATEGIC VIEW SCALING:
                                // 1. Base size depends on island size (star count)
                                // 2. Zoom Factor: As zoom decreases (zooming out), we scale the text UP significantly
                                //    to make it dominate the map view like a political map.
                                const zoomFactor = Math.max(1, 0.6 / view.zoom);
                                const baseSize = 10 + label.count * 2;
                                const fontSize = Math.min(
                                    120,
                                    Math.max(16, baseSize * zoomFactor)
                                );

                                // DYNAMIC OPACITY:
                                // Fade out completely when zooming in to allow viewing individual systems clearly.
                                // Visible (0.8) when zoomed out, starts fading at zoom 0.7, gone by zoom 1.5.
                                const labelOpacity = Math.max(
                                    0,
                                    Math.min(0.8, 1.5 - view.zoom)
                                );

                                if (labelOpacity <= 0.05) return null; // Don't render if effectively invisible

                                return (
                                    <text
                                        key={`label-${empire.id}-${label.key}`}
                                        x={label.x}
                                        y={label.y}
                                        textAnchor="middle"
                                        fill={empire.color}
                                        opacity={labelOpacity}
                                        fontWeight="900" // Extra bold for that "Map Mode" feel
                                        fontSize={fontSize}
                                        style={{
                                            textTransform: "uppercase",
                                            letterSpacing: "0.1em",
                                            pointerEvents: "none",
                                            textShadow:
                                                "0 4px 20px rgba(0,0,0,0.9)", // Heavier shadow for contrast
                                            dominantBaseline: "middle",
                                            transition:
                                                "font-size 0.2s ease-out, opacity 0.2s ease-out", // Smooth scaling & fading
                                        }}
                                    >
                                        {empire.name}
                                    </text>
                                );
                            });
                        })}

                        {/* Selection Box */}
                        {selectionBox && (
                            <rect
                                x={Math.min(
                                    selectionBox.start.x,
                                    selectionBox.current.x
                                )}
                                y={Math.min(
                                    selectionBox.start.y,
                                    selectionBox.current.y
                                )}
                                width={Math.abs(
                                    selectionBox.current.x -
                                        selectionBox.start.x
                                )}
                                height={Math.abs(
                                    selectionBox.current.y -
                                        selectionBox.start.y
                                )}
                                fill="rgba(99, 102, 241, 0.1)"
                                stroke="#6366f1"
                                strokeWidth={1 / view.zoom}
                                strokeDasharray="4,2"
                            />
                        )}
                    </g>
                </svg>

                {/* Hint Overlay */}
                <div className="absolute bottom-4 left-4 text-slate-500 text-xs select-none bg-slate-900/50 p-2 rounded backdrop-blur-sm">
                    {tool === "select" &&
                        "Click to select. Shift+Click to multi-select. Drag empty space to box select."}
                    {tool === "add" && "Click anywhere to spawn a star system."}
                    {tool === "paint" &&
                        "Click or Drag over stars to assign them to the selected empire."}
                    {tool === "link" &&
                        (linkStartId
                            ? "Click another star to connect."
                            : "Click a star to start a hyperlane.")}
                    {tool === "pan" && "Drag to move the view."}
                    <div className="opacity-50 mt-1">
                        Space + Drag to Pan anytime.
                    </div>
                </div>
            </div>

            {/* --- Inspector Panel (Right Side) --- */}
            <SystemInspector
                selectedSystem={singleSelectedSystem}
                selectedCount={selectedIds.size}
                empires={empires}
                onUpdateSystem={updateSystem}
                onDeleteSelected={deleteSelected}
                onClearSelection={() => setSelectedIds(new Set())}
                onBulkUpdateEmpire={handleBulkUpdateEmpire}
            />
        </div>
    );
}
