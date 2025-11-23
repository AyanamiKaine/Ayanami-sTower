import React, { useState, useMemo, useRef, useEffect } from "react";
import {
    Users,
    Heart,
    Share2,
    Activity,
    Dna,
    Save,
    Upload,
    Download,
    Plus,
    Trash2,
    GitCommit,
    UserPlus,
    Shield,
    Crown,
    Scroll,
    Network,
    Tag,
    AlertTriangle,
    X,
    ZoomIn,
    ZoomOut,
    Move,
} from "lucide-react";

// Color mapping for tailwind class names used in relationship types
const TAILWIND_COLORS = {
    "text-blue-400": "#60a5fa",
    "text-rose-400": "#fb7185",
    "text-emerald-400": "#34d399",
    "text-amber-400": "#f59e0b",
    "text-red-500": "#ef4444",
    "text-purple-400": "#a78bfa",
    "text-cyan-400": "#22d3ee",
    "text-pink-400": "#f472b6",
    "text-slate-400": "#94a3b8",
};

// --- Utility Components ---

const Card = ({ children, className = "" }) => (
    <div
        className={`bg-slate-800 border border-slate-700 rounded-lg shadow-sm ${className}`}
    >
        {children}
    </div>
);

const Button = ({
    onClick,
    variant = "primary",
    className = "",
    children,
    ...props
}) => {
    const baseStyle =
        "px-4 py-2 rounded-md font-medium transition-colors flex items-center gap-2 text-sm";
    const variants = {
        primary: "bg-indigo-600 hover:bg-indigo-700 text-white",
        secondary: "bg-slate-700 hover:bg-slate-600 text-slate-200",
        danger: "bg-red-900/50 hover:bg-red-900/70 text-red-200 border border-red-800",
        success: "bg-emerald-600 hover:bg-emerald-700 text-white",
        outline: "border border-slate-600 text-slate-300 hover:bg-slate-700",
    };

    return (
        <button
            onClick={onClick}
            className={`${baseStyle} ${variants[variant]} ${className}`}
            {...props}
        >
            {children}
        </button>
    );
};

const Input = ({ label, value, onChange, type = "text", className = "" }) => (
    <div className={`flex flex-col gap-1 ${className}`}>
        {label && (
            <label className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
                {label}
            </label>
        )}
        <input
            type={type}
            value={value}
            onChange={onChange}
            className="bg-slate-900 border border-slate-700 rounded p-2 text-slate-100 focus:outline-none focus:border-indigo-500 transition-colors"
        />
    </div>
);

const Select = ({ label, value, onChange, options, className = "" }) => (
    <div className={`flex flex-col gap-1 ${className}`}>
        {label && (
            <label className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
                {label}
            </label>
        )}
        <select
            value={value}
            onChange={onChange}
            className="bg-slate-900 border border-slate-700 rounded p-2 text-slate-100 focus:outline-none focus:border-indigo-500 transition-colors appearance-none"
        >
            {options.map((opt) => (
                <option key={opt.value} value={opt.value}>
                    {opt.label}
                </option>
            ))}
        </select>
    </div>
);

const RelationshipGraph = ({
    characters,
    relationships,
    relTypes,
    visibleTypes = [],
}) => {
    const canvasRef = useRef(null);
    const containerRef = useRef(null);
    const [zoom, setZoom] = useState(1);
    const [offset, setOffset] = useState({ x: 0, y: 0 });
    const [dimensions, setDimensions] = useState({ width: 800, height: 600 });

    // Simulation State
    const nodesRef = useRef([]);
    const isDragging = useRef(false);
    const draggedNodeIdx = useRef(null);
    const lastMousePos = useRef({ x: 0, y: 0 });

    // Handle Resize to fix Aspect Ratio & Blurriness
    useEffect(() => {
        if (!containerRef.current) return;
        const observer = new ResizeObserver((entries) => {
            const { width, height } = entries[0].contentRect;
            setDimensions({ width, height });
        });
        observer.observe(containerRef.current);
        return () => observer.disconnect();
    }, []);

    // Initialize Nodes - Sync with props but preserve position
    useEffect(() => {
        // Use current dimensions for initial placement if needed
        const width = dimensions.width;
        const height = dimensions.height;

        const existingNodes = nodesRef.current;

        // Map new props to nodes, preserving x/y if ID matches
        nodesRef.current = characters.map((char) => {
            const existing = existingNodes.find((n) => n.id === char.id);
            return {
                ...char,
                x: existing ? existing.x : Math.random() * width,
                y: existing ? existing.y : Math.random() * height,
                vx: existing ? existing.vx : 0,
                vy: existing ? existing.vy : 0,
            };
        });
    }, [characters, dimensions.width, dimensions.height]);

    // Simulation Loop
    useEffect(() => {
        const canvas = canvasRef.current;
        if (!canvas) return;

        // High DPI Scaling
        const dpr = window.devicePixelRatio || 1;
        canvas.width = dimensions.width * dpr;
        canvas.height = dimensions.height * dpr;

        const ctx = canvas.getContext("2d");
        let animationFrameId;

        const tick = () => {
            const { width, height } = dimensions; // Use logical dimensions for physics
            const nodes = nodesRef.current;

            // 1. Physics Calculations

            // Repulsion (Nodes push apart)
            for (let i = 0; i < nodes.length; i++) {
                for (let j = i + 1; j < nodes.length; j++) {
                    const dx = nodes[i].x - nodes[j].x;
                    const dy = nodes[i].y - nodes[j].y;
                    const dist = Math.sqrt(dx * dx + dy * dy) || 1;
                    const force = 2500 / (dist * dist);

                    const fx = (dx / dist) * force;
                    const fy = (dy / dist) * force;

                    nodes[i].vx += fx;
                    nodes[i].vy += fy;
                    nodes[j].vx -= fx;
                    nodes[j].vy -= fy;
                }
            }

            // Attraction (Edges pull together)
            relationships.forEach((rel) => {
                const source = nodes.find((n) => n.id === rel.source);
                const target = nodes.find((n) => n.id === rel.target);
                if (source && target) {
                    const dx = target.x - source.x;
                    const dy = target.y - source.y;
                    const dist = Math.sqrt(dx * dx + dy * dy) || 1;
                    const force = (dist - 180) * 0.004;

                    const fx = (dx / dist) * force;
                    const fy = (dy / dist) * force;

                    source.vx += fx;
                    source.vy += fy;
                    target.vx -= fx;
                    target.vy -= fy;
                }
            });

            // Center Gravity
            nodes.forEach((node) => {
                node.vx += (width / 2 - node.x) * 0.0015;
                node.vy += (height / 2 - node.y) * 0.0015;
                node.vx *= 0.9;
                node.vy *= 0.9;

                if (draggedNodeIdx.current !== nodes.indexOf(node)) {
                    node.x += node.vx;
                    node.y += node.vy;
                }
            });

            // 2. Rendering
            // Clear using physical pixels to ensure full clear
            ctx.clearRect(0, 0, canvas.width, canvas.height);
            ctx.save();

            // Scale for High DPI
            ctx.scale(dpr, dpr);

            // Apply User Zoom/Pan
            ctx.translate(offset.x, offset.y);
            ctx.scale(zoom, zoom);

            // Draw Connections
            relationships.forEach((rel) => {
                const source = nodes.find((n) => n.id === rel.source);
                const target = nodes.find((n) => n.id === rel.target);
                const type = relTypes.find((rt) => rt.id === rel.typeId);
                // If visibleTypes is provided (array), only show edges whose typeId appears in it.
                // If the array is empty, show none.
                if (Array.isArray(visibleTypes)) {
                    if (visibleTypes.length === 0) return;
                    const set = new Set(visibleTypes);
                    if (!set.has(rel.typeId)) return;
                }
                if (source && target) {
                    ctx.beginPath();
                    ctx.moveTo(source.x, source.y);
                    ctx.lineTo(target.x, target.y);
                    ctx.strokeStyle = type
                        ? TAILWIND_COLORS[type.color] || "#fff"
                        : "#666";
                    ctx.lineWidth = 2 / zoom;
                    ctx.stroke();

                    // Draw Arrow
                    if (type && !type.isSymmetric) {
                        const angle = Math.atan2(
                            target.y - source.y,
                            target.x - source.x
                        );
                        const arrowSize = 8 / zoom;
                        const endX = (source.x + target.x) / 2;
                        const endY = (source.y + target.y) / 2;

                        ctx.save();
                        ctx.translate(endX, endY);
                        ctx.rotate(angle);
                        ctx.fillStyle = ctx.strokeStyle;
                        ctx.beginPath();
                        ctx.moveTo(0, 0);
                        ctx.lineTo(-arrowSize, -arrowSize / 2);
                        ctx.lineTo(-arrowSize, arrowSize / 2);
                        ctx.fill();
                        ctx.restore();
                    }
                }
            });

            // Draw Nodes
            nodes.forEach((node) => {
                const radius = 20 / zoom;
                ctx.beginPath();
                ctx.arc(node.x, node.y, radius, 0, Math.PI * 2);
                ctx.fillStyle = "#1e293b";
                ctx.fill();
                ctx.strokeStyle = "#6366f1";
                ctx.lineWidth = 2 / zoom;
                ctx.stroke();

                // Initials
                ctx.fillStyle = "#fff";
                // Rounding calculated font size to integer helps sharpness
                const fontSize = Math.round(Math.max(10, 14 / zoom));
                ctx.font = `bold ${fontSize}px sans-serif`;
                ctx.textAlign = "center";
                ctx.textBaseline = "middle";
                // Rounding coordinates snaps text to pixels
                ctx.fillText(
                    node.name.substring(0, 2).toUpperCase(),
                    Math.round(node.x),
                    Math.round(node.y)
                );

                // Name Label
                ctx.fillStyle = "#cbd5e1";
                const labelSize = Math.round(Math.max(10, 12 / zoom));
                ctx.font = `${labelSize}px sans-serif`;
                ctx.fillText(
                    node.name,
                    Math.round(node.x),
                    Math.round(node.y + radius + 12 / zoom)
                );
            });

            ctx.restore();
            animationFrameId = requestAnimationFrame(tick);
        };

        tick();
        return () => cancelAnimationFrame(animationFrameId);
    }, [relationships, relTypes, zoom, offset, dimensions, visibleTypes]);

    // Input Handlers
    const handleMouseDown = (e) => {
        const rect = canvasRef.current.getBoundingClientRect();
        // Correct coordinates based on DPR isn't needed for logic, just CSS pixels
        const mouseX = (e.clientX - rect.left - offset.x) / zoom;
        const mouseY = (e.clientY - rect.top - offset.y) / zoom;

        const clickedNodeIdx = nodesRef.current.findIndex((node) => {
            const dist = Math.sqrt(
                (node.x - mouseX) ** 2 + (node.y - mouseY) ** 2
            );
            return dist < 25 / zoom;
        });

        if (clickedNodeIdx !== -1) {
            draggedNodeIdx.current = clickedNodeIdx;
        } else {
            isDragging.current = true;
        }
        lastMousePos.current = { x: e.clientX, y: e.clientY };
    };

    const handleMouseMove = (e) => {
        if (
            draggedNodeIdx.current !== null &&
            nodesRef.current[draggedNodeIdx.current]
        ) {
            const rect = canvasRef.current.getBoundingClientRect();
            nodesRef.current[draggedNodeIdx.current].x =
                (e.clientX - rect.left - offset.x) / zoom;
            nodesRef.current[draggedNodeIdx.current].y =
                (e.clientY - rect.top - offset.y) / zoom;
            nodesRef.current[draggedNodeIdx.current].vx = 0;
            nodesRef.current[draggedNodeIdx.current].vy = 0;
        } else if (isDragging.current) {
            const dx = e.clientX - lastMousePos.current.x;
            const dy = e.clientY - lastMousePos.current.y;
            setOffset((prev) => ({ x: prev.x + dx, y: prev.y + dy }));
            lastMousePos.current = { x: e.clientX, y: e.clientY };
        }
    };

    const handleMouseUp = () => {
        isDragging.current = false;
        draggedNodeIdx.current = null;
    };

    const handleWheel = (e) => {
        e.preventDefault();
        const zoomSensitivity = 0.001;
        const delta = -e.deltaY * zoomSensitivity;
        const newZoom = Math.min(Math.max(zoom + delta, 0.1), 5);

        // Zoom towards pointer
        const rect = canvasRef.current.getBoundingClientRect();
        const mouseX = e.clientX - rect.left;
        const mouseY = e.clientY - rect.top;

        // 1. Get cursor position in the world before zoom
        const worldX = (mouseX - offset.x) / zoom;
        const worldY = (mouseY - offset.y) / zoom;

        // 2. Calculate new offset so that the world position is still under cursor
        const newOffsetX = mouseX - worldX * newZoom;
        const newOffsetY = mouseY - worldY * newZoom;

        setZoom(newZoom);
        setOffset({ x: newOffsetX, y: newOffsetY });
    };

    return (
        <div
            ref={containerRef}
            className="flex flex-col h-full w-full bg-slate-950 relative overflow-hidden"
        >
            <div className="absolute bottom-4 right-4 flex flex-col gap-2 z-10">
                <button
                    onClick={() => setZoom((z) => Math.min(z + 0.1, 3))}
                    className="p-2 bg-slate-800 text-slate-300 rounded hover:bg-slate-700 border border-slate-600 shadow-lg"
                >
                    <ZoomIn size={20} />
                </button>
                <button
                    onClick={() => setZoom((z) => Math.max(z - 0.1, 0.2))}
                    className="p-2 bg-slate-800 text-slate-300 rounded hover:bg-slate-700 border border-slate-600 shadow-lg"
                >
                    <ZoomOut size={20} />
                </button>
                <button
                    onClick={() => {
                        setOffset({ x: 0, y: 0 });
                        setZoom(1);
                    }}
                    className="p-2 bg-slate-800 text-slate-300 rounded hover:bg-slate-700 border border-slate-600 shadow-lg"
                    title="Reset View"
                >
                    <Move size={20} />
                </button>
            </div>

            <canvas
                ref={canvasRef}
                className="w-full h-full cursor-move touch-none block"
                onMouseDown={handleMouseDown}
                onMouseMove={handleMouseMove}
                onMouseUp={handleMouseUp}
                onMouseLeave={handleMouseUp}
                onWheel={handleWheel}
            />
        </div>
    );
};

// --- Main Application ---

export default function CharacterArchitect() {
    const [activeTab, setActiveTab] = useState("characters");
    const [notification, setNotification] = useState(null);
    const [searchQuery, setSearchQuery] = useState("");
    const [newCharacterName, setNewCharacterName] = useState("");
    const quickInputRef = useRef(null);
    const [traitSearchQuery, setTraitSearchQuery] = useState("");
    // Species, cultures and religions loaded from economy defaults (names)
    const [speciesList, setSpeciesList] = useState([]);
    const [culturesList, setCulturesList] = useState([]);
    const [religionsList, setReligionsList] = useState([]);

    // --- Data Models ---

    // 1. Relationship Types (loaded from public defaults)
    const [relTypes, setRelTypes] = useState([]);

    // Which relationship types are visible in the graph (array of type IDs)
    // Default: show none
    const [visibleRelTypes, setVisibleRelTypes] = useState(() => []);

    // 2. Trait Definitions (New System) (loaded from public defaults)
    const [traitDefs, setTraitDefs] = useState([]);

    const filteredTraitDefs = useMemo(() => {
        const q = (traitSearchQuery || "").trim().toLowerCase();
        if (!q) return traitDefs;
        return traitDefs.filter((t) => {
            const name = (t.name || "").toLowerCase();
            const id = (t.id || "").toLowerCase();
            const desc = (t.description || "").toLowerCase();
            return name.includes(q) || id.includes(q) || desc.includes(q);
        });
    }, [traitDefs, traitSearchQuery]);

    const [characters, setCharacters] = useState([]);

    // 4. Relationships (The Edges)
    const [relationships, setRelationships] = useState([]);

    // Fetch defaults from /defaults/character-defaults.json on mount
    useEffect(() => {
        const url = "/defaults/character-defaults.json";
        fetch(url)
            .then((res) => {
                if (!res.ok)
                    throw new Error("Failed to fetch character defaults");
                return res.json();
            })
            .then((data) => {
                if (data.relTypes) setRelTypes(data.relTypes);
                if (data.traitDefs) setTraitDefs(data.traitDefs);
                if (data.characters) setCharacters(data.characters);
                if (data.relationships) setRelationships(data.relationships);
            })
            .catch((err) => {
                console.warn("character defaults fetch error", err);
            });

        // Also fetch species from economy defaults so we can enforce selection
        fetch("/defaults/economy-defaults.json")
            .then((res) => {
                if (!res.ok)
                    throw new Error("Failed to fetch economy defaults");
                return res.json();
            })
            .then((data) => {
                if (data.species)
                    setSpeciesList(data.species.map((s) => s.name));
                if (data.cultures)
                    setCulturesList(data.cultures.map((c) => c.name));
                if (data.religions)
                    setReligionsList(data.religions.map((r) => r.name));
            })
            .catch((err) => {
                console.warn("economy defaults fetch error", err);
            });
    }, []);

    // --- Logic Helpers ---

    const generateId = (prefix) =>
        `${prefix}_${Math.random().toString(36).substr(2, 9)}`;

    const showNotification = (msg) => {
        setNotification(msg);
        setTimeout(() => setNotification(null), 3000);
    };

    const getTraitName = (id) => traitDefs.find((t) => t.id === id)?.name || id;

    // --- Handlers: Traits ---
    const addTraitDef = () => {
        setTraitDefs([
            ...traitDefs,
            {
                id: generateId("t"),
                name: "New Trait",
                opposites: [],
                description: "",
            },
        ]);
    };

    const updateTraitDef = (id, field, value) => {
        setTraitDefs(
            traitDefs.map((t) => (t.id === id ? { ...t, [field]: value } : t))
        );
    };

    const toggleTraitOpposite = (traitId, oppositeId) => {
        // Toggle for primary trait and ensure reciprocity
        setTraitDefs((prev) => {
            const next = prev.map((t) => {
                if (t.id !== traitId) return t;
                const isPresent = t.opposites.includes(oppositeId);
                return {
                    ...t,
                    opposites: isPresent
                        ? t.opposites.filter((o) => o !== oppositeId)
                        : [...t.opposites, oppositeId],
                };
            });

            // Now ensure reciprocal update on 'opposite' trait
            return next.map((t) => {
                if (t.id !== oppositeId) return t;
                const target = next.find((n) => n.id === traitId);
                const isPresent = t.opposites.includes(traitId);
                const shouldBePresent =
                    target.opposites.includes(oppositeId) ||
                    target.opposites.includes(traitId);
                // If the traitId now includes oppositeId (we toggled on) => ensure reciprocal exists
                // If we toggled off, ensure reciprocal is removed
                const primaryHasOpposite = next
                    .find((x) => x.id === traitId)
                    .opposites.includes(oppositeId);
                if (primaryHasOpposite && !isPresent) {
                    return { ...t, opposites: [...t.opposites, traitId] };
                }
                if (!primaryHasOpposite && isPresent) {
                    return {
                        ...t,
                        opposites: t.opposites.filter((o) => o !== traitId),
                    };
                }
                return t;
            });
        });
    };

    const deleteTraitDef = (id) => {
        if (
            !confirm(
                "Delete this trait? It will be removed from all characters."
            )
        )
            return;
        setTraitDefs((prev) =>
            prev
                .filter((t) => t.id !== id)
                .map((t) => ({
                    ...t,
                    opposites: t.opposites.filter((o) => o !== id),
                }))
        );
        // Remove from characters
        setCharacters(
            characters.map((c) => ({
                ...c,
                traits: c.traits.filter((tid) => tid !== id),
            }))
        );
        // (Reciprocal cleanup performed in the single update above)
    };

    // --- Handlers: Relationship Types ---
    const addRelType = () => {
        setRelTypes([
            ...relTypes,
            {
                id: generateId("rt"),
                name: "New Relation",
                reverseName: "Reverse Relation",
                color: "text-slate-400",
                isSymmetric: false,
                description: "",
            },
        ]);
    };

    const updateRelType = (id, field, value) => {
        setRelTypes(
            relTypes.map((rt) =>
                rt.id === id ? { ...rt, [field]: value } : rt
            )
        );
    };

    // --- Handlers: Characters ---
    const addCharacter = (name = null) => {
        setCharacters([
            ...characters,
            {
                id: generateId("char"),
                name: name || "New Character",
                age: 25,
                species: speciesList[0] || "Human",
                culture: culturesList[0] || "",
                religion: religionsList[0] || "",
                attributes: {
                    martial: 5,
                    diplomacy: 5,
                    stewardship: 5,
                    intrigue: 5,
                },
                traits: [],
            },
        ]);
    };

    const updateCharacter = (id, field, value) => {
        setCharacters(
            characters.map((c) => (c.id === id ? { ...c, [field]: value } : c))
        );
    };

    const updateAttribute = (charId, attr, value) => {
        setCharacters(
            characters.map((c) => {
                if (c.id !== charId) return c;
                return {
                    ...c,
                    attributes: {
                        ...c.attributes,
                        [attr]: parseInt(value) || 0,
                    },
                };
            })
        );
    };

    const addCharacterTrait = (charId, traitId) => {
        if (!traitId) return;

        const char = characters.find((c) => c.id === charId);
        const trait = traitDefs.find((t) => t.id === traitId);

        // Check if already has trait
        if (char.traits.includes(traitId)) return;

        // Check conflicts
        const conflicts = trait.opposites.filter((opId) =>
            char.traits.includes(opId)
        );
        if (conflicts.length > 0) {
            const conflictNames = conflicts.map(getTraitName).join(", ");
            showNotification(
                `Cannot add '${trait.name}': Conflicts with '${conflictNames}'`
            );
            return;
        }

        setCharacters(
            characters.map((c) => {
                if (c.id !== charId) return c;
                return { ...c, traits: [...c.traits, traitId] };
            })
        );
    };

    const removeCharacterTrait = (charId, traitId) => {
        setCharacters(
            characters.map((c) => {
                if (c.id !== charId) return c;
                return { ...c, traits: c.traits.filter((t) => t !== traitId) };
            })
        );
    };

    const deleteCharacter = (id) => {
        if (
            !confirm(
                "Delete character? This will remove all their relationships."
            )
        )
            return;
        setCharacters(characters.filter((c) => c.id !== id));
        setRelationships(
            relationships.filter((r) => r.source !== id && r.target !== id)
        );
    };

    // --- Handlers: Connections ---
    const addConnection = (sourceId, targetId, typeId) => {
        if (sourceId === targetId)
            return showNotification("Cannot link character to self");
        const exists = relationships.find(
            (r) =>
                (r.source === sourceId &&
                    r.target === targetId &&
                    r.typeId === typeId) ||
                (r.source === targetId &&
                    r.target === sourceId &&
                    r.typeId === typeId) // Check distinct pairs
        );
        if (exists) return showNotification("Relationship already exists");

        setRelationships([
            ...relationships,
            {
                id: generateId("rel"),
                source: sourceId,
                target: targetId,
                typeId,
            },
        ]);
    };

    const removeConnection = (relId) => {
        setRelationships(relationships.filter((r) => r.id !== relId));
    };

    // --- Renderers ---

    const renderTraits = () => (
        <div className="space-y-6">
            <div className="flex justify-between items-center">
                <div>
                    <h2 className="text-2xl font-bold text-white">
                        Trait Registry
                    </h2>
                    <p className="text-slate-400">
                        Define personality types and mutual exclusions.
                    </p>
                </div>
                <div className="flex items-center gap-2">
                    <input
                        className="bg-slate-900 text-sm p-2 rounded border border-slate-700 text-slate-300 focus:border-indigo-500"
                        placeholder="Search traits (name/id/desc)..."
                        value={traitSearchQuery}
                        onChange={(e) => setTraitSearchQuery(e.target.value)}
                        aria-label="Search traits"
                    />
                    <div className="text-xs text-slate-400">
                        {filteredTraitDefs.length} / {traitDefs.length}
                    </div>
                    <Button onClick={addTraitDef}>
                        <Plus size={16} /> New Trait
                    </Button>
                </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {filteredTraitDefs.map((trait) => (
                    <Card key={trait.id} className="p-4">
                        <div className="flex justify-between items-start mb-3">
                            <div className="flex items-center gap-2 flex-1">
                                <Tag className="text-indigo-400" size={18} />
                                <Input
                                    value={trait.name}
                                    onChange={(e) =>
                                        updateTraitDef(
                                            trait.id,
                                            "name",
                                            e.target.value
                                        )
                                    }
                                    className="w-full max-w-xs font-bold"
                                />
                            </div>
                            <button
                                onClick={() => deleteTraitDef(trait.id)}
                                className="text-slate-500 hover:text-red-400"
                            >
                                <Trash2 size={16} />
                            </button>
                        </div>

                        <Input
                            label="Description"
                            value={trait.description}
                            onChange={(e) =>
                                updateTraitDef(
                                    trait.id,
                                    "description",
                                    e.target.value
                                )
                            }
                            className="mb-4"
                        />

                        <div className="bg-slate-900/50 p-3 rounded border border-slate-700/50">
                            <label className="text-xs font-semibold text-slate-400 uppercase tracking-wider mb-2 block flex items-center gap-1">
                                <AlertTriangle
                                    size={12}
                                    className="text-amber-500"
                                />{" "}
                                Opposing Traits (Conflicts)
                            </label>
                            <div className="flex flex-wrap gap-2">
                                {traitDefs
                                    .filter((t) => t.id !== trait.id)
                                    .map((other) => {
                                        const isOpposite =
                                            trait.opposites.includes(other.id);
                                        return (
                                            <button
                                                key={other.id}
                                                onClick={() =>
                                                    toggleTraitOpposite(
                                                        trait.id,
                                                        other.id
                                                    )
                                                }
                                                className={`px-2 py-1 rounded text-xs border transition-colors ${
                                                    isOpposite
                                                        ? "bg-red-900/40 border-red-800 text-red-200"
                                                        : "bg-slate-800 border-slate-700 text-slate-400 hover:border-slate-500"
                                                }`}
                                            >
                                                {other.name}
                                            </button>
                                        );
                                    })}
                            </div>
                            <p className="text-[10px] text-slate-500 mt-2 italic">
                                Characters with '{trait.name}' cannot be
                                assigned traits highlighted in red.
                            </p>
                        </div>
                    </Card>
                ))}
            </div>
        </div>
    );

    const renderRelTypes = () => (
        <div className="space-y-6">
            <div className="flex justify-between items-center">
                <div>
                    <h2 className="text-2xl font-bold text-white">
                        Relationship Schemas
                    </h2>
                    <p className="text-slate-400">
                        Define the biology and sociology of your galaxy.
                    </p>
                </div>
                <Button onClick={addRelType}>
                    <Plus size={16} /> New Type
                </Button>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
                {relTypes.map((rt) => (
                    <Card key={rt.id} className="p-4">
                        <div className="flex justify-between items-start mb-4">
                            <div className="flex items-center gap-2">
                                <div
                                    className={`p-2 rounded bg-slate-900 ${rt.color}`}
                                >
                                    <Share2 size={20} />
                                </div>
                                <Input
                                    value={rt.name}
                                    onChange={(e) =>
                                        updateRelType(
                                            rt.id,
                                            "name",
                                            e.target.value
                                        )
                                    }
                                    className="w-48 font-bold"
                                />
                            </div>
                            <button
                                onClick={() =>
                                    setRelTypes(
                                        relTypes.filter((r) => r.id !== rt.id)
                                    )
                                }
                                className="text-slate-500 hover:text-red-400"
                            >
                                <Trash2 size={16} />
                            </button>
                        </div>

                        <div className="space-y-3 bg-slate-900/50 p-3 rounded border border-slate-700/50">
                            <div className="flex items-center gap-4">
                                <label className="flex items-center gap-2 cursor-pointer">
                                    <input
                                        type="checkbox"
                                        checked={rt.isSymmetric}
                                        onChange={(e) =>
                                            updateRelType(
                                                rt.id,
                                                "isSymmetric",
                                                e.target.checked
                                            )
                                        }
                                        className="rounded bg-slate-800 border-slate-600 text-indigo-500 focus:ring-indigo-500"
                                    />
                                    <span className="text-sm text-slate-300">
                                        Symmetric (Bidirectional)
                                    </span>
                                </label>
                            </div>

                            {!rt.isSymmetric && (
                                <Input
                                    label="Reverse Label (If A is Parent, B is...)"
                                    value={rt.reverseName}
                                    onChange={(e) =>
                                        updateRelType(
                                            rt.id,
                                            "reverseName",
                                            e.target.value
                                        )
                                    }
                                />
                            )}

                            <Input
                                label="Description"
                                value={rt.description}
                                onChange={(e) =>
                                    updateRelType(
                                        rt.id,
                                        "description",
                                        e.target.value
                                    )
                                }
                            />

                            <div className="mt-2">
                                <label className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
                                    Color Theme
                                </label>
                                <div className="flex gap-2 mt-1">
                                    {[
                                        "text-blue-400",
                                        "text-rose-400",
                                        "text-emerald-400",
                                        "text-amber-400",
                                        "text-red-500",
                                        "text-purple-400",
                                    ].map((c) => (
                                        <button
                                            key={c}
                                            onClick={() =>
                                                updateRelType(rt.id, "color", c)
                                            }
                                            className={`w-6 h-6 rounded-full border-2 ${c.replace(
                                                "text",
                                                "bg"
                                            )} ${
                                                rt.color === c
                                                    ? "border-white"
                                                    : "border-transparent"
                                            }`}
                                        />
                                    ))}
                                </div>
                            </div>
                        </div>
                    </Card>
                ))}
            </div>
        </div>
    );

    const renderGraph = () => (
        <div className="space-y-4">
            <div className="flex justify-between items-center">
                <div>
                    <h2 className="text-2xl font-bold text-white">
                        Relationship Graph
                    </h2>
                    <p className="text-slate-400">
                        Interactive graph view of your characters and their
                        relationships.
                    </p>
                </div>
            </div>

            <Card className="p-0 bg-slate-900/30 border border-slate-700">
                <div className="w-full h-[64vh] flex flex-col md:flex-row gap-4">
                    <div className="hidden md:block md:w-72 p-3 border-r border-slate-800 overflow-auto">
                        <div className="flex justify-between items-center mb-3">
                            <h4 className="text-sm text-slate-300 font-bold">
                                Legend
                            </h4>
                            <div className="flex gap-1">
                                <button
                                    onClick={() =>
                                        setVisibleRelTypes(
                                            relTypes.map((r) => r.id)
                                        )
                                    }
                                    className="text-xs text-slate-300 hover:text-white"
                                >
                                    All
                                </button>
                                <button
                                    onClick={() => setVisibleRelTypes([])}
                                    className="text-xs text-slate-300 hover:text-white ml-2"
                                >
                                    None
                                </button>
                            </div>
                        </div>
                        <div className="flex flex-col gap-2">
                            {relTypes.map((rt) => (
                                <label
                                    key={rt.id}
                                    className="flex items-center gap-2 text-sm text-slate-300"
                                >
                                    <input
                                        type="checkbox"
                                        checked={visibleRelTypes.includes(
                                            rt.id
                                        )}
                                        onChange={(e) => {
                                            const next = new Set(
                                                visibleRelTypes
                                            );
                                            if (e.target.checked)
                                                next.add(rt.id);
                                            else next.delete(rt.id);
                                            setVisibleRelTypes(
                                                Array.from(next)
                                            );
                                        }}
                                    />
                                    <span
                                        className={`inline-block w-3 h-3 rounded ${rt.color}`}
                                        style={{
                                            backgroundColor:
                                                TAILWIND_COLORS[rt.color],
                                        }}
                                    />
                                    <span>{rt.name}</span>
                                </label>
                            ))}
                        </div>
                    </div>
                    <div className="flex-1 w-full">
                        <div className="w-full h-full">
                            <RelationshipGraph
                                characters={characters}
                                relationships={relationships}
                                relTypes={relTypes}
                                visibleTypes={visibleRelTypes}
                            />
                        </div>
                    </div>
                </div>
            </Card>
        </div>
    );

    const filteredCharacters = useMemo(() => {
        const q = (searchQuery || "").trim().toLowerCase();
        if (!q) return characters;
        return characters.filter((c) => {
            const name = c.name?.toLowerCase() || "";
            const species = (c.species || "").toLowerCase();
            const culture = (c.culture || "").toLowerCase();
            const religion = (c.religion || "").toLowerCase();
            const id = (c.id || "").toLowerCase();
            const traits = (c.traits || [])
                .map((t) => t.toLowerCase())
                .join(" ");
            return (
                name.includes(q) ||
                species.includes(q) ||
                culture.includes(q) ||
                religion.includes(q) ||
                id.includes(q) ||
                traits.includes(q)
            );
        });
    }, [characters, searchQuery]);

    const renderCharacters = () => (
        <div className="space-y-6">
            <div className="flex justify-between items-center">
                <div>
                    <h2 className="text-2xl font-bold text-white">
                        Character Registry
                    </h2>
                    <p className="text-slate-400">Define Characters.</p>
                </div>
                <div className="flex items-center gap-2">
                    <div className="hidden sm:flex gap-2 items-center">
                        <input
                            ref={quickInputRef}
                            value={newCharacterName}
                            onChange={(e) =>
                                setNewCharacterName(e.target.value)
                            }
                            onKeyDown={(e) => {
                                if (e.key === "Enter") {
                                    addCharacter(newCharacterName || null);
                                    setNewCharacterName("");
                                    setSearchQuery(newCharacterName || "");
                                    // Focus back to search
                                    quickInputRef.current?.focus();
                                }
                            }}
                            placeholder="New character name"
                            className="bg-slate-900 border border-slate-700 rounded p-2 text-slate-100 text-sm focus:outline-none focus:border-indigo-500"
                            aria-label="New character name"
                        />
                        <Button
                            variant="secondary"
                            onClick={() => {
                                addCharacter(newCharacterName || null);
                                setSearchQuery(newCharacterName || "");
                                setNewCharacterName("");
                                quickInputRef.current?.focus();
                            }}
                        >
                            <UserPlus size={14} /> Create
                        </Button>
                    </div>
                    <Button onClick={() => addCharacter()}>
                        <UserPlus size={16} /> New Character
                    </Button>
                </div>
                <div className="flex items-center gap-2 ml-4">
                    <input
                        className="bg-slate-900 text-sm p-2 rounded border border-slate-700 text-slate-300 focus:border-indigo-500"
                        placeholder="Search characters (name/species/id)..."
                        value={searchQuery}
                        onChange={(e) => setSearchQuery(e.target.value)}
                        aria-label="Search characters"
                    />
                    <div className="text-xs text-slate-400">
                        {filteredCharacters.length} / {characters.length}
                    </div>
                </div>
            </div>

            <div className="grid grid-cols-1 gap-4">
                {filteredCharacters.map((char) => (
                    <Card
                        key={char.id}
                        className="p-4 flex flex-col lg:flex-row gap-6"
                    >
                        {/* Left: Bio & Stats */}
                        <div className="flex-1 space-y-4">
                            <div className="flex justify-between items-start">
                                <div className="flex items-center gap-3">
                                    <div className="w-12 h-12 rounded-full bg-slate-700 flex items-center justify-center text-slate-400 font-bold text-xl border-2 border-slate-600">
                                        {char.name.charAt(0)}
                                    </div>
                                    <div>
                                        <Input
                                            value={char.name}
                                            onChange={(e) =>
                                                updateCharacter(
                                                    char.id,
                                                    "name",
                                                    e.target.value
                                                )
                                            }
                                            className="font-bold text-lg w-full md:w-64"
                                        />
                                        <div className="flex gap-2 mt-1">
                                            <input
                                                type="number"
                                                value={char.age}
                                                onChange={(e) =>
                                                    updateCharacter(
                                                        char.id,
                                                        "age",
                                                        parseInt(e.target.value)
                                                    )
                                                }
                                                className="w-16 bg-slate-900 border border-slate-700 rounded px-2 py-0.5 text-xs text-slate-300"
                                                placeholder="Age"
                                            />
                                            {speciesList.length > 0 ? (
                                                <select
                                                    value={char.species}
                                                    onChange={(e) =>
                                                        updateCharacter(
                                                            char.id,
                                                            "species",
                                                            e.target.value
                                                        )
                                                    }
                                                    className="w-24 bg-slate-900 border border-slate-700 rounded px-2 py-0.5 text-xs text-slate-300"
                                                >
                                                    {speciesList.map((s) => (
                                                        <option
                                                            key={s}
                                                            value={s}
                                                        >
                                                            {s}
                                                        </option>
                                                    ))}
                                                </select>
                                            ) : (
                                                <input
                                                    value={char.species}
                                                    onChange={(e) =>
                                                        updateCharacter(
                                                            char.id,
                                                            "species",
                                                            e.target.value
                                                        )
                                                    }
                                                    className="w-24 bg-slate-900 border border-slate-700 rounded px-2 py-0.5 text-xs text-slate-300"
                                                    placeholder="Species"
                                                />
                                            )}

                                            {/* Culture select/input */}
                                            {culturesList.length > 0 ? (
                                                <select
                                                    value={char.culture || ""}
                                                    onChange={(e) =>
                                                        updateCharacter(
                                                            char.id,
                                                            "culture",
                                                            e.target.value
                                                        )
                                                    }
                                                    className="w-28 bg-slate-900 border border-slate-700 rounded px-2 py-0.5 text-xs text-slate-300"
                                                >
                                                    <option value="">
                                                        -- Culture --
                                                    </option>
                                                    {culturesList.map((c) => (
                                                        <option
                                                            key={c}
                                                            value={c}
                                                        >
                                                            {c}
                                                        </option>
                                                    ))}
                                                </select>
                                            ) : (
                                                <input
                                                    value={char.culture || ""}
                                                    onChange={(e) =>
                                                        updateCharacter(
                                                            char.id,
                                                            "culture",
                                                            e.target.value
                                                        )
                                                    }
                                                    className="w-28 bg-slate-900 border border-slate-700 rounded px-2 py-0.5 text-xs text-slate-300"
                                                    placeholder="Culture"
                                                />
                                            )}

                                            {/* Religion select/input */}
                                            {religionsList.length > 0 ? (
                                                <select
                                                    value={char.religion || ""}
                                                    onChange={(e) =>
                                                        updateCharacter(
                                                            char.id,
                                                            "religion",
                                                            e.target.value
                                                        )
                                                    }
                                                    className="w-28 bg-slate-900 border border-slate-700 rounded px-2 py-0.5 text-xs text-slate-300"
                                                >
                                                    <option value="">
                                                        -- Religion --
                                                    </option>
                                                    {religionsList.map((r) => (
                                                        <option
                                                            key={r}
                                                            value={r}
                                                        >
                                                            {r}
                                                        </option>
                                                    ))}
                                                </select>
                                            ) : (
                                                <input
                                                    value={char.religion || ""}
                                                    onChange={(e) =>
                                                        updateCharacter(
                                                            char.id,
                                                            "religion",
                                                            e.target.value
                                                        )
                                                    }
                                                    className="w-28 bg-slate-900 border border-slate-700 rounded px-2 py-0.5 text-xs text-slate-300"
                                                    placeholder="Religion"
                                                />
                                            )}
                                        </div>
                                    </div>
                                </div>
                                <button
                                    onClick={() => deleteCharacter(char.id)}
                                    className="text-slate-600 hover:text-red-400"
                                >
                                    <Trash2 size={16} />
                                </button>
                            </div>

                            {/* Attributes Grid */}
                            <div className="bg-slate-900/50 p-3 rounded-lg border border-slate-700/50">
                                <h4 className="text-xs font-bold text-slate-500 uppercase mb-2 flex items-center gap-1">
                                    <Activity size={12} />
                                    Base Attributes
                                </h4>
                                <div className="grid grid-cols-4 gap-2 text-center">
                                    {[
                                        {
                                            id: "martial",
                                            label: "MAR",
                                            icon: Shield,
                                            color: "text-red-400",
                                        },
                                        {
                                            id: "diplomacy",
                                            label: "DIP",
                                            icon: Crown,
                                            color: "text-blue-400",
                                        },
                                        {
                                            id: "stewardship",
                                            label: "STE",
                                            icon: Scroll,
                                            color: "text-emerald-400",
                                        },
                                        {
                                            id: "intrigue",
                                            label: "INT",
                                            icon: Dna,
                                            color: "text-purple-400",
                                        },
                                    ].map((attr) => (
                                        <div
                                            key={attr.id}
                                            className="flex flex-col items-center"
                                        >
                                            <attr.icon
                                                size={14}
                                                className={`mb-1 ${attr.color}`}
                                            />
                                            <label className="text-[10px] text-slate-500 font-bold">
                                                {attr.label}
                                            </label>
                                            <input
                                                type="number"
                                                value={char.attributes[attr.id]}
                                                onChange={(e) =>
                                                    updateAttribute(
                                                        char.id,
                                                        attr.id,
                                                        e.target.value
                                                    )
                                                }
                                                className="w-full text-center bg-transparent border-b border-slate-700 focus:border-indigo-500 focus:outline-none text-slate-200 font-mono"
                                            />
                                        </div>
                                    ))}
                                </div>
                            </div>

                            {/* Traits (Updated Selection System) */}
                            <div className="bg-slate-900/30 p-2 rounded border border-slate-700/30">
                                <div className="flex justify-between items-center mb-2">
                                    <label className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
                                        Personality Traits
                                    </label>
                                </div>

                                <div className="flex flex-wrap gap-2 mb-3">
                                    {char.traits.map((traitId) => {
                                        const trait = traitDefs.find(
                                            (t) => t.id === traitId
                                        );
                                        return (
                                            <span
                                                key={traitId}
                                                className="flex items-center gap-1 px-2 py-1 rounded-full bg-indigo-900/40 text-indigo-200 text-xs border border-indigo-700/50"
                                            >
                                                {trait
                                                    ? trait.name
                                                    : "Unknown Trait"}
                                                <button
                                                    onClick={() =>
                                                        removeCharacterTrait(
                                                            char.id,
                                                            traitId
                                                        )
                                                    }
                                                    className="hover:text-white"
                                                >
                                                    <X size={10} />
                                                </button>
                                            </span>
                                        );
                                    })}
                                </div>

                                <div className="relative">
                                    <select
                                        className="w-full bg-slate-900 text-xs p-2 rounded border border-slate-700 text-slate-300 focus:border-indigo-500"
                                        value=""
                                        onChange={(e) =>
                                            addCharacterTrait(
                                                char.id,
                                                e.target.value
                                            )
                                        }
                                    >
                                        <option value="">+ Add Trait...</option>
                                        {traitDefs
                                            .filter(
                                                (t) =>
                                                    !char.traits.includes(t.id)
                                            ) // Filter out already added
                                            .map((t) => (
                                                <option key={t.id} value={t.id}>
                                                    {t.name}
                                                </option>
                                            ))}
                                    </select>
                                </div>
                            </div>
                        </div>

                        {/* Right: Relationships / Connections */}
                        <div className="flex-1 bg-slate-900/30 border-l border-slate-700 pl-6 border-dashed">
                            <h4 className="text-sm font-bold text-slate-300 mb-3 flex items-center gap-2">
                                <Network size={16} /> Connections
                            </h4>

                            <div className="space-y-2 max-h-48 overflow-y-auto mb-4 custom-scrollbar pr-2">
                                {relationships.filter(
                                    (r) =>
                                        r.source === char.id ||
                                        r.target === char.id
                                ).length === 0 && (
                                    <p className="text-sm text-slate-600 italic">
                                        No relations defined.
                                    </p>
                                )}
                                {relationships
                                    .filter(
                                        (r) =>
                                            r.source === char.id ||
                                            r.target === char.id
                                    )
                                    .map((rel) => {
                                        const isSource = rel.source === char.id;
                                        const otherId = isSource
                                            ? rel.target
                                            : rel.source;
                                        const otherChar = characters.find(
                                            (c) => c.id === otherId
                                        );
                                        const type = relTypes.find(
                                            (rt) => rt.id === rel.typeId
                                        );

                                        if (!otherChar || !type) return null;

                                        return (
                                            <div
                                                key={rel.id}
                                                className="flex justify-between items-center bg-slate-900 p-2 rounded border border-slate-800 text-sm"
                                            >
                                                <div className="flex items-center gap-2">
                                                    <span
                                                        className={`text-xs font-bold uppercase ${type.color}`}
                                                    >
                                                        {isSource
                                                            ? type.name
                                                            : type.isSymmetric
                                                            ? type.name
                                                            : type.reverseName}
                                                    </span>
                                                    <span className="text-slate-500">
                                                        &rarr;
                                                    </span>
                                                    <span className="text-slate-200">
                                                        {otherChar.name}
                                                    </span>
                                                </div>
                                                <button
                                                    onClick={() =>
                                                        removeConnection(rel.id)
                                                    }
                                                    className="text-slate-600 hover:text-red-400"
                                                >
                                                    <Trash2 size={12} />
                                                </button>
                                            </div>
                                        );
                                    })}
                            </div>

                            {/* Add Connection Control */}
                            <div className="bg-slate-800 p-2 rounded border border-slate-700">
                                <div className="text-[10px] font-bold text-slate-500 uppercase mb-1">
                                    Add Relationship
                                </div>
                                <div className="flex flex-col gap-2">
                                    <select
                                        id={`new-rel-type-${char.id}`}
                                        className="bg-slate-900 text-xs p-1.5 rounded text-slate-300 border border-slate-700"
                                    >
                                        {relTypes.map((rt) => (
                                            <option key={rt.id} value={rt.id}>
                                                {rt.name}
                                            </option>
                                        ))}
                                    </select>
                                    <select
                                        id={`new-rel-target-${char.id}`}
                                        className="bg-slate-900 text-xs p-1.5 rounded text-slate-300 border border-slate-700"
                                    >
                                        <option value="">
                                            Select Character...
                                        </option>
                                        {characters
                                            .filter((c) => c.id !== char.id)
                                            .map((c) => (
                                                <option key={c.id} value={c.id}>
                                                    {c.name}
                                                </option>
                                            ))}
                                    </select>
                                    <Button
                                        variant="secondary"
                                        className="justify-center py-1 text-xs"
                                        onClick={() => {
                                            const typeId =
                                                document.getElementById(
                                                    `new-rel-type-${char.id}`
                                                ).value;
                                            const targetId =
                                                document.getElementById(
                                                    `new-rel-target-${char.id}`
                                                ).value;
                                            if (targetId)
                                                addConnection(
                                                    char.id,
                                                    targetId,
                                                    typeId
                                                );
                                        }}
                                    >
                                        Link
                                    </Button>
                                </div>
                            </div>
                        </div>
                    </Card>
                ))}
            </div>
        </div>
    );

    const renderData = () => (
        <div className="max-w-2xl mx-auto space-y-6">
            <Card className="p-6 text-center space-y-4">
                <h3 className="text-xl font-bold text-white">
                    Import / Export
                </h3>
                <p className="text-slate-400 text-sm">
                    Save your character database to JSON for use in Stella
                    Invicta.
                </p>

                <div className="grid grid-cols-2 gap-4">
                    <Button
                        className="justify-center"
                        onClick={() => {
                            const data = JSON.stringify(
                                {
                                    characters,
                                    relationships,
                                    relTypes,
                                    traitDefs,
                                },
                                null,
                                2
                            );
                            const blob = new Blob([data], {
                                type: "application/json",
                            });
                            const url = URL.createObjectURL(blob);
                            const a = document.createElement("a");
                            a.href = url;
                            a.download = "stella_invicta_characters.json";
                            document.body.appendChild(a);
                            a.click();
                            document.body.removeChild(a);
                        }}
                    >
                        <Download size={16} /> Export JSON
                    </Button>

                    <label className="flex items-center justify-center px-4 py-2 bg-slate-700 hover:bg-slate-600 text-slate-200 rounded-md cursor-pointer transition-colors gap-2 font-medium text-sm">
                        <Upload size={16} /> Import JSON
                        <input
                            type="file"
                            className="hidden"
                            accept=".json"
                            onChange={(e) => {
                                const file = e.target.files[0];
                                if (!file) return;
                                const reader = new FileReader();
                                reader.onload = (ev) => {
                                    try {
                                        const json = JSON.parse(
                                            ev.target.result
                                        );
                                        if (json.characters)
                                            setCharacters(json.characters);
                                        if (json.relationships)
                                            setRelationships(
                                                json.relationships
                                            );
                                        if (json.relTypes)
                                            setRelTypes(json.relTypes);
                                        if (json.traitDefs)
                                            setTraitDefs(json.traitDefs);
                                        showNotification("Database Loaded");
                                    } catch (err) {
                                        showNotification("Error parsing JSON");
                                    }
                                };
                                reader.readAsText(file);
                            }}
                        />
                    </label>
                </div>
            </Card>

            <div className="bg-slate-950 p-4 rounded-lg border border-slate-800 font-mono text-xs text-slate-300 overflow-auto max-h-96">
                <pre>
                    {JSON.stringify(
                        { characters, relationships, relTypes, traitDefs },
                        null,
                        2
                    )}
                </pre>
            </div>
        </div>
    );

    return (
        <div className="min-h-screen bg-slate-950 text-slate-200 font-sans selection:bg-indigo-500/30">
            {/* Header */}
            <header className="bg-slate-900 border-b border-slate-800 sticky top-0 z-10">
                <div className="max-w-6xl mx-auto px-4 h-16 flex items-center justify-between">
                    <div className="flex items-center gap-3">
                        <div className="w-8 h-8 bg-gradient-to-br from-indigo-500 to-rose-600 rounded-lg flex items-center justify-center shadow-lg shadow-indigo-500/20">
                            <Crown className="text-white" size={18} />
                        </div>
                        <h1 className="font-bold text-xl tracking-tight text-white">
                            Stella Invicta{" "}
                            <span className="text-slate-500 font-normal">
                                | Dynasty Architect
                            </span>
                        </h1>
                    </div>

                    <div className="flex items-center gap-1 bg-slate-800 p-1 rounded-lg">
                        <button
                            onClick={() => setActiveTab("characters")}
                            className={`px-3 py-2 rounded-md text-sm font-medium flex items-center gap-2 transition-all ${
                                activeTab === "characters"
                                    ? "bg-slate-700 text-white"
                                    : "text-slate-400 hover:bg-slate-800"
                            }`}
                        >
                            <Users size={16} /> Characters
                        </button>
                        <button
                            onClick={() => setActiveTab("traits")}
                            className={`px-3 py-2 rounded-md text-sm font-medium flex items-center gap-2 transition-all ${
                                activeTab === "traits"
                                    ? "bg-slate-700 text-white"
                                    : "text-slate-400 hover:bg-slate-800"
                            }`}
                        >
                            <Tag size={16} /> Traits
                        </button>
                        <button
                            onClick={() => setActiveTab("schema")}
                            className={`px-3 py-2 rounded-md text-sm font-medium flex items-center gap-2 transition-all ${
                                activeTab === "schema"
                                    ? "bg-slate-700 text-white"
                                    : "text-slate-400 hover:bg-slate-800"
                            }`}
                        >
                            <GitCommit size={16} /> Rel. Types
                        </button>
                        <button
                            onClick={() => setActiveTab("graph")}
                            className={`px-3 py-2 rounded-md text-sm font-medium flex items-center gap-2 transition-all ${
                                activeTab === "graph"
                                    ? "bg-slate-700 text-white"
                                    : "text-slate-400 hover:bg-slate-800"
                            }`}
                        >
                            <Network size={16} /> Graph
                        </button>
                        <button
                            onClick={() => setActiveTab("data")}
                            className={`px-3 py-2 rounded-md text-sm font-medium flex items-center gap-2 transition-all ${
                                activeTab === "data"
                                    ? "bg-slate-700 text-white"
                                    : "text-slate-400 hover:bg-slate-800"
                            }`}
                        >
                            <Save size={16} /> Data
                        </button>
                    </div>
                </div>
            </header>

            <main className="max-w-6xl mx-auto px-4 py-8">
                {notification && (
                    <div className="fixed bottom-6 right-6 px-6 py-3 rounded-lg shadow-xl bg-indigo-900/90 border border-indigo-700 text-white z-50 animate-bounce">
                        {notification}
                    </div>
                )}

                <div className="animate-in fade-in slide-in-from-bottom-4 duration-300">
                    {activeTab === "characters" && renderCharacters()}
                    {activeTab === "traits" && renderTraits()}
                    {activeTab === "schema" && renderRelTypes()}
                    {activeTab === "graph" && renderGraph()}
                    {activeTab === "data" && renderData()}
                </div>
            </main>
        </div>
    );
}
