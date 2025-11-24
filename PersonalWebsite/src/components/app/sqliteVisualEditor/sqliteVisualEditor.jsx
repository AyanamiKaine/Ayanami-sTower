import React, { useState, useEffect, useRef, useMemo } from "react";
import {
    ReactFlow,
    Background,
    Controls,
    // MiniMap removed, using Controls and Background only
    useNodesState,
    useEdgesState,
    addEdge,
    Handle,
    Position,
    // useReactFlow removed - we'll use reactFlowRef.current.project() instead
    ReactFlowProvider,
} from "@xyflow/react";
import "@xyflow/react/dist/style.css";
// Local flow control custom theme for better contrast on dark background
import "./sqliteVisualEditor.css";
import {
    Plus,
    Trash2,
    Database,
    Code,
    X,
    GripHorizontal,
    Key,
    Link as LinkIcon,
    Download,
    Copy,
    RefreshCw,
    Table as TableIcon,
    AlertTriangle,
    Gamepad2,
    Cpu,
    StickyNote,
    Map as MapIcon,
} from "lucide-react";

/**
 * UTILITIES & CONSTANTS
 */
const DATA_TYPES = ["INTEGER", "TEXT", "BLOB", "REAL", "NUMERIC"];

const generateId = () => Math.random().toString(36).substr(2, 9);

const INITIAL_SCHEMA = [
    {
        id: "entities",
        name: "entities",
        x: 100,
        y: 300,
        notes: "Core <b>ECS Registry</b>.<br/>Defines all unique objects in the scene. <a href='https://stellawiki.com/docs/Engine/state' style='color: #d8b4fe'>View Docs</a>",
        columns: [
            {
                id: "e1",
                name: "id",
                type: "INTEGER",
                pk: true,
                ai: true,
                notNull: true,
                unique: false,
                def: "",
            },
            {
                id: "e2",
                name: "uid",
                type: "TEXT",
                pk: false,
                ai: false,
                notNull: true,
                unique: true,
                def: "uuid()",
            },
            {
                id: "e3",
                name: "tag",
                type: "TEXT",
                pk: false,
                ai: false,
                notNull: false,
                unique: false,
                def: "'untagged'",
            },
        ],
    },
    {
        id: "transform",
        name: "c_transform",
        x: 500,
        y: 100,
        notes: "Spatial data component.",
        columns: [
            {
                id: "t1",
                name: "entity_id",
                type: "INTEGER",
                pk: true,
                ai: false,
                notNull: true,
                unique: true,
                def: "",
                fk: { tableId: "entities", colId: "e1" },
            },
            {
                id: "t2",
                name: "pos_x",
                type: "REAL",
                pk: false,
                ai: false,
                notNull: true,
                unique: false,
                def: "0.0",
            },
            {
                id: "t3",
                name: "pos_y",
                type: "REAL",
                pk: false,
                ai: false,
                notNull: true,
                unique: false,
                def: "0.0",
            },
            {
                id: "t4",
                name: "rot_z",
                type: "REAL",
                pk: false,
                ai: false,
                notNull: true,
                unique: false,
                def: "0.0",
            },
        ],
    },
    {
        id: "stats",
        name: "c_stats",
        x: 500,
        y: 300,
        notes: "",
        columns: [
            {
                id: "s1",
                name: "entity_id",
                type: "INTEGER",
                pk: true,
                ai: false,
                notNull: true,
                unique: true,
                def: "",
                fk: { tableId: "entities", colId: "e1" },
            },
            {
                id: "s2",
                name: "hp_current",
                type: "INTEGER",
                pk: false,
                ai: false,
                notNull: true,
                unique: false,
                def: "100",
            },
            {
                id: "s3",
                name: "hp_max",
                type: "INTEGER",
                pk: false,
                ai: false,
                notNull: true,
                unique: false,
                def: "100",
            },
            {
                id: "s4",
                name: "mana",
                type: "INTEGER",
                pk: false,
                ai: false,
                notNull: true,
                unique: false,
                def: "50",
            },
        ],
    },
    {
        id: "items",
        name: "item_defs",
        x: 100,
        y: 600,
        notes: "Static definitions for items.",
        columns: [
            {
                id: "i1",
                name: "id",
                type: "INTEGER",
                pk: true,
                ai: true,
                notNull: true,
                unique: false,
                def: "",
            },
            {
                id: "i2",
                name: "name",
                type: "TEXT",
                pk: false,
                ai: false,
                notNull: true,
                unique: true,
                def: "",
            },
            {
                id: "i3",
                name: "rarity",
                type: "INTEGER",
                pk: false,
                ai: false,
                notNull: true,
                unique: false,
                def: "1",
            },
            {
                id: "i4",
                name: "icon_path",
                type: "TEXT",
                pk: false,
                ai: false,
                notNull: false,
                unique: false,
                def: "",
            },
        ],
    },
    {
        id: "inventory",
        name: "c_inventory",
        x: 500,
        y: 550,
        notes: "N:M Link between Entities and Items.",
        columns: [
            {
                id: "inv1",
                name: "entity_id",
                type: "INTEGER",
                pk: true,
                ai: false,
                notNull: true,
                unique: false,
                def: "",
                fk: { tableId: "entities", colId: "e1" },
            },
            {
                id: "inv2",
                name: "item_id",
                type: "INTEGER",
                pk: true,
                ai: false,
                notNull: true,
                unique: false,
                def: "",
                fk: { tableId: "items", colId: "i1" },
            },
            {
                id: "inv3",
                name: "amount",
                type: "INTEGER",
                pk: false,
                ai: false,
                notNull: true,
                unique: false,
                def: "1",
            },
            {
                id: "inv4",
                name: "is_equipped",
                type: "INTEGER",
                pk: false,
                ai: false,
                notNull: true,
                unique: false,
                def: "0",
            },
        ],
    },
];

/**
 * HOOKS
 */
const useLocalStorage = (key, initialValue) => {
    const isBrowser = typeof window !== "undefined";
    const [value, setValue] = useState(() => {
        if (!isBrowser) return initialValue;
        try {
            const item = window.localStorage.getItem(key);
            return item ? JSON.parse(item) : initialValue;
        } catch (error) {
            console.error(error);
            return initialValue;
        }
    });

    // Keep local storage in sync when value changes on the client
    useEffect(() => {
        if (!isBrowser) return;
        try {
            window.localStorage.setItem(key, JSON.stringify(value));
        } catch (error) {
            console.error(error);
        }
    }, [isBrowser, key, value]);

    const setValueWrap = (valueToStore) => {
        try {
            const valueToSave =
                valueToStore instanceof Function
                    ? valueToStore(value)
                    : valueToStore;
            setValue(valueToSave);
            if (isBrowser) {
                try {
                    window.localStorage.setItem(
                        key,
                        JSON.stringify(valueToSave)
                    );
                } catch (error) {
                    console.error(error);
                }
            }
        } catch (error) {
            console.error(error);
        }
    };

    return [value, setValueWrap];
};

/**
 * COMPONENTS
 */

// --- Column Editor Row ---
const ColumnRow = ({ col, tables, currentTableId, onDelete, onChange }) => {
    const [isExpanded, setIsExpanded] = useState(false);
    const otherTables = tables.filter((t) => t.id !== currentTableId);

    const handleChange = (field, value) => {
        onChange({ ...col, [field]: value });
    };

    const handleFkChange = (tableId) => {
        if (!tableId) {
            const { fk, ...rest } = col;
            onChange(rest);
            return;
        }
        const targetTable = tables.find((t) => t.id === tableId);
        const targetCol =
            targetTable.columns.find((c) => c.pk) || targetTable.columns[0];

        onChange({
            ...col,
            fk: { tableId, colId: targetCol?.id },
        });
    };

    return (
        <div className="border-b border-zinc-800 last:border-0 bg-zinc-900/40">
            <div className="flex items-center gap-2 p-2 group">
                <div className="cursor-move text-zinc-600 hover:text-zinc-400">
                    <GripHorizontal size={14} />
                </div>

                <div className="flex gap-1 min-w-[32px]">
                    <button
                        onClick={() => handleChange("pk", !col.pk)}
                        className={`transition-colors ${
                            col.pk
                                ? "text-amber-400"
                                : "text-zinc-600 hover:text-zinc-400"
                        }`}
                        title="Primary Key"
                    >
                        <Key
                            size={14}
                            className={col.pk ? "fill-current" : ""}
                        />
                    </button>
                    <button
                        onClick={() => setIsExpanded(!isExpanded)}
                        className={`transition-colors ${
                            col.fk
                                ? "text-cyan-400"
                                : "text-zinc-600 hover:text-zinc-400"
                        }`}
                        title="Foreign Key / Details"
                    >
                        <LinkIcon size={14} />
                    </button>
                </div>

                <input
                    type="text"
                    value={col.name}
                    onChange={(e) => handleChange("name", e.target.value)}
                    placeholder="col_name"
                    className="bg-transparent text-sm text-zinc-200 focus:outline-none focus:ring-1 focus:ring-fuchsia-500 rounded px-1 py-0.5 w-full font-mono"
                />

                <select
                    value={col.type}
                    onChange={(e) => handleChange("type", e.target.value)}
                    className="bg-zinc-950 text-xs text-fuchsia-300 border border-zinc-700 rounded px-1 py-1 focus:outline-none focus:border-fuchsia-500 font-mono w-24"
                >
                    {DATA_TYPES.map((t) => (
                        <option key={t} value={t}>
                            {t}
                        </option>
                    ))}
                </select>

                <button
                    onClick={onDelete}
                    className="text-zinc-600 hover:text-red-400 opacity-0 group-hover:opacity-100 transition-opacity"
                >
                    <Trash2 size={14} />
                </button>
            </div>

            {isExpanded && (
                <div className="p-3 bg-zinc-950/50 grid grid-cols-2 gap-3 text-xs border-t border-zinc-800 animate-in slide-in-from-top-2 duration-200">
                    <div className="space-y-2">
                        <label className="flex items-center gap-2 text-zinc-400 cursor-pointer hover:text-zinc-200">
                            <input
                                type="checkbox"
                                checked={col.notNull}
                                onChange={(e) =>
                                    handleChange("notNull", e.target.checked)
                                }
                                className="rounded border-zinc-600 bg-zinc-800 text-fuchsia-500 focus:ring-0 focus:ring-offset-0 w-3.5 h-3.5"
                            />
                            NOT NULL
                        </label>
                        <label className="flex items-center gap-2 text-zinc-400 cursor-pointer hover:text-zinc-200">
                            <input
                                type="checkbox"
                                checked={col.unique}
                                onChange={(e) =>
                                    handleChange("unique", e.target.checked)
                                }
                                className="rounded border-zinc-600 bg-zinc-800 text-fuchsia-500 focus:ring-0 focus:ring-offset-0 w-3.5 h-3.5"
                            />
                            UNIQUE
                        </label>
                        <label className="flex items-center gap-2 text-zinc-400 cursor-pointer hover:text-zinc-200">
                            <input
                                type="checkbox"
                                checked={col.ai}
                                disabled={col.type !== "INTEGER" || !col.pk}
                                onChange={(e) =>
                                    handleChange("ai", e.target.checked)
                                }
                                className="rounded border-zinc-600 bg-zinc-800 text-fuchsia-500 focus:ring-0 focus:ring-offset-0 w-3.5 h-3.5 disabled:opacity-50"
                            />
                            AUTO INCREMENT
                        </label>
                    </div>

                    <div className="space-y-2">
                        <div>
                            <span className="text-zinc-500 block mb-1">
                                Default Value
                            </span>
                            <input
                                type="text"
                                value={col.def}
                                onChange={(e) =>
                                    handleChange("def", e.target.value)
                                }
                                placeholder="NULL"
                                className="w-full bg-zinc-800 border border-zinc-600 rounded px-2 py-1 text-zinc-200 focus:border-fuchsia-500 focus:outline-none"
                            />
                        </div>

                        <div>
                            <span className="text-zinc-500 block mb-1">
                                Foreign Key To
                            </span>
                            <div className="flex gap-2">
                                <select
                                    value={col.fk?.tableId || ""}
                                    onChange={(e) =>
                                        handleFkChange(e.target.value)
                                    }
                                    className="w-full bg-zinc-800 border border-zinc-600 rounded px-2 py-1 text-zinc-200 focus:border-fuchsia-500 focus:outline-none"
                                >
                                    <option value="">-- None --</option>
                                    {otherTables.map((t) => (
                                        <option key={t.id} value={t.id}>
                                            {t.name}
                                        </option>
                                    ))}
                                </select>
                                {col.fk?.tableId && (
                                    <button
                                        onClick={() => handleFkChange("")}
                                        className="p-1 hover:bg-red-900/30 text-zinc-500 hover:text-red-400 rounded transition-colors"
                                        title="Remove Foreign Key"
                                    >
                                        <X size={14} />
                                    </button>
                                )}
                            </div>
                        </div>

                        {col.fk?.tableId && (
                            <div>
                                <span className="text-zinc-500 block mb-1">
                                    Target Column
                                </span>
                                <select
                                    value={col.fk.colId}
                                    onChange={(e) =>
                                        handleChange("fk", {
                                            ...col.fk,
                                            colId: e.target.value,
                                        })
                                    }
                                    className="w-full bg-zinc-800 border border-zinc-600 rounded px-2 py-1 text-zinc-200 focus:border-fuchsia-500 focus:outline-none"
                                >
                                    {tables
                                        .find((t) => t.id === col.fk.tableId)
                                        ?.columns.map((c) => (
                                            <option key={c.id} value={c.id}>
                                                {c.name}
                                            </option>
                                        ))}
                                </select>
                            </div>
                        )}
                    </div>
                </div>
            )}
        </div>
    );
};

// --- Table Node (Canvas) ---
// --- Table Node (Canvas) ---
const TableNode = ({ id, data, selected }) => {
    const table = data.table;
    const notesRef = useRef(null);

    useEffect(() => {
        const container = notesRef.current;
        if (!container) return;
        const anchors = Array.from(container.querySelectorAll("a"));
        const handler = (e) => {
            // stop React Flow from handling the click and open the URL explicitly
            e.stopPropagation();
            e.preventDefault();
            const a = e.currentTarget;
            let href = a.getAttribute("href") || a.href;
            if (!href) return;
            // if it's just a hash (#) or anchor, try data-url attribute first
            if (href === "#" || href.startsWith("#")) {
                const fallback = a.getAttribute("data-url");
                if (fallback) {
                    const url = /^(https?:|mailto:|tel:|\/)/i.test(fallback)
                        ? fallback
                        : `https://${fallback}`;
                    window.open(url, "_blank", "noopener,noreferrer");
                }
                return;
            }

            // normalize href: if it lacks protocol, make it absolute
            if (!/^(https?:|mailto:|tel:|\/\/)/i.test(href)) {
                if (href.startsWith("/")) href = window.location.origin + href;
                else href = `https://${href}`;
            }
            window.open(href, "_blank", "noopener,noreferrer");
        };
        anchors.forEach((a) => {
            try {
                a.setAttribute("target", "_blank");
                a.setAttribute("rel", "noopener noreferrer");
            } catch (err) {
                // ignore
            }
            a.addEventListener("click", handler);
        });
        return () => {
            anchors.forEach((a) => a.removeEventListener("click", handler));
        };
    }, [table.notes]);
    return (
        <div
            className={`w-64 bg-zinc-900/90 backdrop-blur-sm rounded-lg shadow-2xl overflow-hidden cursor-pointer select-none transition-colors duration-200
        ${
            selected
                ? "ring-2 ring-fuchsia-500 shadow-fuchsia-900/40 z-10"
                : "border border-zinc-800 hover:border-zinc-600 hover:shadow-xl"
        }`}
        >
            <div
                className={`px-3 py-2 flex items-center gap-2 border-b border-zinc-800 ${
                    selected ? "bg-fuchsia-900/20" : "bg-zinc-950/50"
                }`}
            >
                {table.name.startsWith("c_") ? (
                    <Cpu size={14} className="text-cyan-400" />
                ) : (
                    <TableIcon size={14} className="text-fuchsia-400" />
                )}
                <span className="font-bold text-zinc-100 text-sm truncate flex-1 font-mono tracking-tight">
                    {table.name}
                </span>
                {table.notes && (
                    <StickyNote size={12} className="text-yellow-500" />
                )}
                <button
                    onClick={(e) => {
                        e.stopPropagation();
                        data.onDelete?.(table.id);
                    }}
                    className="text-zinc-600 hover:text-red-400 p-1 hover:bg-zinc-800 rounded transition-colors"
                >
                    <Trash2 size={12} />
                </button>
            </div>

            <div className="p-2 space-y-1 max-h-[250px] overflow-y-auto custom-scrollbar">
                {table.columns.map((col, idx) => (
                    <div
                        key={col.id}
                        className="flex items-center justify-between text-xs group px-1 rounded hover:bg-zinc-800/50"
                    >
                        <div className="flex items-center gap-2 overflow-hidden">
                            <div className="flex w-3 flex-shrink-0 justify-center">
                                {col.pk && (
                                    <Key size={10} className="text-amber-400" />
                                )}
                                {!col.pk && col.fk && (
                                    <LinkIcon
                                        size={10}
                                        className="text-cyan-400"
                                    />
                                )}
                            </div>
                            <span
                                className={`truncate font-mono ${
                                    col.pk
                                        ? "text-zinc-100 font-bold"
                                        : "text-zinc-400"
                                }`}
                            >
                                {col.name}
                            </span>
                        </div>
                        <span className="text-zinc-600 font-mono text-[10px] uppercase tracking-wider">
                            {col.type}
                        </span>
                        {/* Left side target handle for PK */}
                        {col.pk && (
                            <Handle
                                type="target"
                                position={Position.Left}
                                id={`${id}-${col.id}-target`}
                                className="bg-amber-500 w-2 h-2"
                                style={{ top: idx * 20 + 24 }}
                            />
                        )}
                        {/* Right side source handle for FK */}
                        {
                            <Handle
                                type="source"
                                position={Position.Right}
                                id={`${id}-${col.id}-source`}
                                className="bg-cyan-400 w-2 h-2"
                                style={{ top: idx * 20 + 24 }}
                            />
                        }
                    </div>
                ))}
            </div>

            {table.notes && (
                <div
                    ref={notesRef}
                    className="px-3 py-2 bg-yellow-900/10 border-t border-yellow-900/20 text-[10px] text-zinc-400 font-sans leading-relaxed break-words"
                    dangerouslySetInnerHTML={{ __html: table.notes }}
                />
            )}

            <div className="px-3 py-1.5 bg-zinc-950/80 border-t border-zinc-800 text-[10px] text-zinc-500 flex justify-between font-mono">
                <span>{table.columns.length} Fields</span>
            </div>
        </div>
    );
};

/**
 * MAIN APP COMPONENT
 */
const AppContent = () => {
    // Use v6 key to reset to new Gaming Schema with Notes
    const [tables, setTables] = useLocalStorage(
        "sqlite-schema-v6",
        INITIAL_SCHEMA
    );
    const [selectedTableId, setSelectedTableId] = useState(null);
    const [showSql, setShowSql] = useState(false);
    const [deleteConfirm, setDeleteConfirm] = useState({
        show: false,
        tableId: null,
    });
    const [nodes, setNodes, onNodesChange] = useNodesState([]);
    const [edges, setEdges, onEdgesChange] = useEdgesState([]);
    const reactFlowRef = useRef(null);
    // Use reactFlowRef.current.project() when needed to map screen coordinates to flow coordinates

    const nodeTypes = useMemo(() => ({ table: TableNode }), []);

    const tablesToNodes = (tables) =>
        tables.map((t) => ({
            id: t.id,
            position: { x: t.x, y: t.y },
            data: { table: t, onDelete: initiateDeleteTable },
            type: "table",
        }));

    const tablesToEdges = (tables) =>
        tables.flatMap((t) =>
            t.columns
                .filter((c) => c.fk)
                .map((c) => {
                    const targetTable = tables.find(
                        (tt) => tt.id === c.fk.tableId
                    );
                    const targetCol = targetTable?.columns.find(
                        (cc) => cc.id === c.fk.colId
                    );
                    const pkCount = t.columns.filter((col) => col.pk).length;
                    const relation =
                        pkCount >= 2 ? "N:M" : c.unique ? "1:1" : "1:N";
                    const style =
                        pkCount >= 2
                            ? {
                                  stroke: "#a78bfa",
                                  strokeWidth: 2,
                                  strokeDasharray: "6 4",
                              }
                            : c.unique
                            ? { stroke: "#84cc16", strokeWidth: 2 }
                            : c.pk
                            ? { stroke: "#f97316", strokeWidth: 2 }
                            : { stroke: "#06b6d4", strokeWidth: 2 };

                    return {
                        id: `${t.id}-${c.id}`,
                        source: t.id,
                        sourceHandle: `${t.id}-${c.id}-source`,
                        target: c.fk.tableId,
                        targetHandle: `${c.fk.tableId}-${c.fk.colId}-target`,
                        animated: false,
                        label: `FK: ${c.name} → ${
                            targetCol?.name || c.fk.colId
                        }`,
                        labelStyle: {
                            fontSize: 12,
                            fill: "#ffffff",
                            fontWeight: 700,
                        },
                        labelBgStyle: {
                            fill: "#0f172a",
                            stroke: "rgba(255,255,255,0.05)",
                            strokeWidth: 0,
                        },
                        labelBgPadding: [8, 6],
                        labelBgBorderRadius: 4,
                        type: "smoothstep",
                        style,
                        data: { relation },
                    };
                })
        );

    // derive nodes/edges from tables only when the table *structure* changes
    const tablesStructureHash = useMemo(
        () =>
            JSON.stringify(
                tables.map((t) => ({
                    id: t.id,
                    name: t.name,
                    columns: t.columns.map((c) => ({
                        id: c.id,
                        name: c.name,
                        type: c.type,
                        pk: !!c.pk,
                        fk: c.fk
                            ? { tableId: c.fk.tableId, colId: c.fk.colId }
                            : null,
                    })),
                }))
            ),
        [tables]
    );

    useEffect(() => {
        setNodes((prevNodes) => {
            const nextNodes = tablesToNodes(tables);
            const prevMap = new Map(prevNodes.map((n) => [n.id, n]));
            return nextNodes.map((n) => {
                const prev = prevMap.get(n.id);
                if (prev) {
                    // preserve all react-flow internal properties by returning prev and only updating data & type
                    return {
                        ...prev,
                        data: { ...prev.data, table: n.data.table },
                        type: n.type,
                    };
                }
                return n;
            });
        });
        setEdges((prevEdges) => {
            const nextEdges = tablesToEdges(tables);
            const prevMap = new Map(prevEdges.map((e) => [e.id, e]));
            return nextEdges.map((e) =>
                prevMap.get(e.id) ? { ...prevMap.get(e.id), ...e } : e
            );
        });
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [tablesStructureHash]);

    // sync node position back to tables
    useEffect(() => {
        setTables((prev) => {
            let changed = false;
            const next = prev.map((t) => {
                const n = nodes.find((nd) => nd.id === t.id);
                if (!n) return t;
                if (t.x !== n.position.x || t.y !== n.position.y) {
                    changed = true;
                    return { ...t, x: n.position.x, y: n.position.y };
                }
                return t;
            });
            return changed ? next : prev;
        });
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [nodes]);

    // Actions
    const addTable = () => {
        const newId = generateId();
        const isBrowser = typeof window !== "undefined";

        // Use reactFlow instance project() to get center of view in flow coordinates
        const screenCenter = {
            x: isBrowser ? window.innerWidth / 2 : 400,
            y: isBrowser ? window.innerHeight / 2 : 300,
        };
        const center = reactFlowRef.current?.project
            ? reactFlowRef.current.project(screenCenter)
            : { x: screenCenter.x, y: screenCenter.y };

        const newTable = {
            id: newId,
            name: `new_entity_${Object.keys(tables).length + 1}`,
            x: center.x - 128,
            y: center.y - 100,
            notes: "",
            columns: [
                {
                    id: generateId(),
                    name: "id",
                    type: "INTEGER",
                    pk: true,
                    ai: true,
                    notNull: true,
                    unique: false,
                    def: "",
                },
            ],
        };
        setTables([...tables, newTable]);
        setSelectedTableId(newId);
    };

    const initiateDeleteTable = (id) => {
        setDeleteConfirm({ show: true, tableId: id });
    };

    const confirmDeleteTable = () => {
        const id = deleteConfirm.tableId;
        if (!id) return;

        setTables((prev) =>
            prev
                .filter((t) => t.id !== id)
                .map((t) => ({
                    ...t,
                    columns: t.columns.map((c) =>
                        c.fk?.tableId === id ? { ...c, fk: null } : c
                    ),
                }))
        );
        if (selectedTableId === id) setSelectedTableId(null);
        setDeleteConfirm({ show: false, tableId: null });
    };

    const updateTable = (id, updates) => {
        setTables((prev) =>
            prev.map((t) => (t.id === id ? { ...t, ...updates } : t))
        );
    };

    const addColumn = (tableId) => {
        const table = tables.find((t) => t.id === tableId);
        const newCol = {
            id: generateId(),
            name: `new_prop`,
            type: "TEXT",
            pk: false,
            ai: false,
            notNull: false,
            unique: false,
            def: "",
        };
        updateTable(tableId, { columns: [...table.columns, newCol] });
    };

    const updateColumn = (tableId, colId, updates) => {
        const table = tables.find((t) => t.id === tableId);
        const newCols = table.columns.map((c) =>
            c.id === colId ? updates : c
        );
        updateTable(tableId, { columns: newCols });
    };

    const deleteColumn = (tableId, colId) => {
        const table = tables.find((t) => t.id === tableId);
        if (table.columns.length === 1)
            return alert("Table must have at least one column.");
        updateTable(tableId, {
            columns: table.columns.filter((c) => c.id !== colId),
        });
    };

    // SQL Generator
    const generateSQL = () => {
        let sql = `-- Generated by SQLite Architect (ECS Mode)\n-- ${new Date().toISOString()}\n\n`;
        sql += `PRAGMA foreign_keys = OFF;\n`;
        [...tables].reverse().forEach((t) => {
            sql += `DROP TABLE IF EXISTS "${t.name}";\n`;
        });
        sql += `PRAGMA foreign_keys = ON;\n\n`;

        tables.forEach((t) => {
            sql += `CREATE TABLE "${t.name}" (\n`;
            const colDefs = [];
            const constraints = [];

            t.columns.forEach((c) => {
                let def = `  "${c.name}" ${c.type}`;
                if (c.notNull) def += " NOT NULL";
                if (c.unique) def += " UNIQUE";
                if (c.def) def += ` DEFAULT ${c.def}`;
                if (c.pk) {
                    def += " PRIMARY KEY";
                    if (c.ai && c.type === "INTEGER") def += " AUTOINCREMENT";
                }
                colDefs.push(def);

                if (c.fk) {
                    const targetTable = tables.find(
                        (tbl) => tbl.id === c.fk.tableId
                    );
                    const targetCol = targetTable?.columns.find(
                        (col) => col.id === c.fk.colId
                    );
                    if (targetTable && targetCol) {
                        constraints.push(
                            `  FOREIGN KEY ("${c.name}") REFERENCES "${targetTable.name}" ("${targetCol.name}") ON DELETE CASCADE`
                        );
                    }
                }
            });

            sql += colDefs.join(",\n");
            if (constraints.length > 0) {
                sql += ",\n" + constraints.join(",\n");
            }
            sql += `\n);\n\n`;
        });

        return sql;
    };

    const selectedTable = useMemo(
        () => tables.find((t) => t.id === selectedTableId),
        [tables, selectedTableId]
    );

    // React Flow handles mouse interactions and dragging; legacy listeners removed

    return (
        <div className="flex h-screen bg-zinc-950 text-zinc-100 overflow-hidden font-sans selection:bg-fuchsia-500/30">
            {/* --- Sidebar --- */}
            <div
                className={`flex flex-col border-r border-zinc-800 bg-zinc-900 shadow-2xl z-30 transition-all duration-300 ${
                    selectedTable
                        ? "w-96 translate-x-0"
                        : "w-0 -translate-x-full opacity-0 overflow-hidden"
                }`}
            >
                {selectedTable && (
                    <>
                        <div className="p-4 border-b border-zinc-800 flex items-center justify-between bg-zinc-900">
                            <h2 className="font-bold flex items-center gap-2 text-fuchsia-400">
                                <Database size={18} />
                                Edit Entity
                            </h2>
                            <button
                                onClick={() => setSelectedTableId(null)}
                                className="text-zinc-500 hover:text-white"
                            >
                                <X size={18} />
                            </button>
                        </div>
                        <div className="p-4 space-y-6 overflow-y-auto flex-1 custom-scrollbar">
                            <div className="space-y-3">
                                <label className="text-xs font-bold text-zinc-500 uppercase tracking-wider">
                                    Entity/Table Name
                                </label>
                                <div className="flex gap-2">
                                    <input
                                        type="text"
                                        value={selectedTable.name}
                                        onChange={(e) =>
                                            updateTable(selectedTable.id, {
                                                name: e.target.value,
                                            })
                                        }
                                        className="flex-1 bg-zinc-950 border border-zinc-700 rounded px-3 py-2 focus:border-fuchsia-500 focus:ring-1 focus:ring-fuchsia-500 outline-none transition-all font-mono"
                                    />
                                    <button
                                        onClick={() =>
                                            initiateDeleteTable(
                                                selectedTable.id
                                            )
                                        }
                                        className="p-2 bg-red-900/10 text-red-400 border border-red-900/20 rounded hover:bg-red-900/20 transition-colors"
                                    >
                                        <Trash2 size={18} />
                                    </button>
                                </div>
                            </div>

                            {/* Notes Editor */}
                            <div className="space-y-3">
                                <label className="text-xs font-bold text-zinc-500 uppercase tracking-wider flex items-center gap-2">
                                    <StickyNote size={14} /> Notes /
                                    Documentation
                                </label>
                                <textarea
                                    value={selectedTable.notes || ""}
                                    onChange={(e) =>
                                        updateTable(selectedTable.id, {
                                            notes: e.target.value,
                                        })
                                    }
                                    placeholder="Supports <b>HTML</b> for bold, links, etc."
                                    className="w-full h-24 bg-zinc-950 border border-zinc-700 rounded px-3 py-2 text-sm focus:border-fuchsia-500 focus:ring-1 focus:ring-fuchsia-500 outline-none transition-all resize-none custom-scrollbar"
                                />
                            </div>

                            <div className="space-y-3">
                                <div className="flex items-center justify-between">
                                    <label className="text-xs font-bold text-zinc-500 uppercase tracking-wider">
                                        Components / Fields
                                    </label>
                                    <button
                                        onClick={() =>
                                            addColumn(selectedTable.id)
                                        }
                                        className="text-xs flex items-center gap-1 text-fuchsia-400 hover:text-fuchsia-300 font-medium"
                                    >
                                        <Plus size={14} /> Add Field
                                    </button>
                                </div>
                                <div className="border border-zinc-700 rounded-lg overflow-hidden">
                                    {selectedTable.columns.map((col) => (
                                        <ColumnRow
                                            key={col.id}
                                            col={col}
                                            tables={tables}
                                            currentTableId={selectedTable.id}
                                            onDelete={() =>
                                                deleteColumn(
                                                    selectedTable.id,
                                                    col.id
                                                )
                                            }
                                            onChange={(updates) =>
                                                updateColumn(
                                                    selectedTable.id,
                                                    col.id,
                                                    updates
                                                )
                                            }
                                        />
                                    ))}
                                </div>
                            </div>
                        </div>
                    </>
                )}
            </div>

            {/* --- Main Canvas --- */}
            <div className="flex-1 relative overflow-hidden bg-[#050505]">
                <div className="absolute top-4 left-4 z-40 flex gap-2">
                    <button
                        onClick={addTable}
                        className="bg-fuchsia-600 hover:bg-fuchsia-500 text-white px-4 py-2 rounded-lg shadow-lg shadow-fuchsia-900/20 flex items-center gap-2 font-medium transition-all"
                    >
                        <Plus size={18} /> New Entity
                    </button>
                    <button
                        onClick={() => setShowSql(true)}
                        className="bg-zinc-800 hover:bg-zinc-700 text-zinc-200 border border-zinc-700 px-4 py-2 rounded-lg shadow-lg flex items-center gap-2 font-medium transition-all"
                    >
                        <Code size={18} /> SQL
                    </button>
                </div>

                {/* Relation Legend */}
                <div className="absolute top-20 left-4 z-40 flex gap-2 items-center text-xs text-zinc-300">
                    <div className="flex items-center gap-2 bg-zinc-900/80 border border-zinc-700 rounded px-3 py-2">
                        <div className="w-3 h-3 bg-[#06b6d4] rounded-sm" />
                        <div>1:N</div>
                        <div className="mx-2">•</div>
                        <div className="w-3 h-3 bg-[#84cc16] rounded-sm" />
                        <div>1:1</div>
                        <div className="mx-2">•</div>
                        <div className="w-3 h-3 bg-[#f97316] rounded-sm" />
                        <div>PK Link</div>
                        <div className="mx-2">•</div>
                        <div className="w-3 h-3 bg-[#a78bfa] rounded-sm border-dashed" />
                        <div>N:M</div>
                    </div>
                </div>

                <div className="absolute top-4 right-4 z-40 flex gap-2">
                    <button
                        onClick={() => setTables(INITIAL_SCHEMA)}
                        className="bg-zinc-800 hover:bg-zinc-700 text-zinc-400 p-2 rounded-lg border border-zinc-700 shadow-lg"
                        title="Reset ECS Schema"
                    >
                        <RefreshCw size={18} />
                    </button>
                </div>

                <div
                    style={{ width: "100%", height: "100%" }}
                    className="absolute z-20"
                >
                    <ReactFlow
                        nodes={nodes}
                        edges={edges}
                        onNodesChange={onNodesChange}
                        onEdgesChange={(changes) => {
                            onEdgesChange(changes);
                            // detect removal of edges to cleanup FK
                            changes.forEach((c) => {
                                if (c.type === "remove") {
                                    const removedEdge = c.item;
                                    // Try to extract the column id from the removed edge's sourceHandle
                                    const src = removedEdge.source;
                                    const srcHandle = removedEdge.sourceHandle;
                                    const sourceColId = srcHandle?.startsWith(
                                        `${src}-`
                                    )
                                        ? srcHandle
                                              .slice(src.length + 1)
                                              .replace(/-source$/, "")
                                        : (srcHandle || "").replace(
                                              /-source$/,
                                              ""
                                          );
                                    setTables((prev) =>
                                        prev.map((t) => ({
                                            ...t,
                                            columns: t.columns.map((col) => {
                                                if (
                                                    t.id === src &&
                                                    col.id === sourceColId
                                                ) {
                                                    const { fk, ...rest } = col;
                                                    return rest;
                                                }
                                                // also fallback: removedEdge.id might match expectedEdgeId (if edges derived from tables)
                                                const expectedEdgeId = `${t.id}-${col.id}`;
                                                if (
                                                    removedEdge.id ===
                                                    expectedEdgeId
                                                ) {
                                                    const { fk, ...rest } = col;
                                                    return rest;
                                                }
                                                return col;
                                            }),
                                        }))
                                    );
                                }
                            });
                        }}
                        onConnect={(params) => {
                            // params.sourceHandle and targetHandle arrive in `${nodeId}-${colId}-source` / `-target` format
                            const {
                                source,
                                sourceHandle,
                                target,
                                targetHandle,
                            } = params;
                            const sourceColId = sourceHandle?.startsWith(
                                `${source}-`
                            )
                                ? sourceHandle
                                      .slice(source.length + 1)
                                      .replace(/-source$/, "")
                                : (sourceHandle || "").replace(/-source$/, "");
                            const targetColId = targetHandle?.startsWith(
                                `${target}-`
                            )
                                ? targetHandle
                                      .slice(target.length + 1)
                                      .replace(/-target$/, "")
                                : (targetHandle || "").replace(/-target$/, "");
                            const sourceTable = tables.find(
                                (t) => t.id === source
                            );
                            const targetTable = tables.find(
                                (t) => t.id === target
                            );
                            const sourceCol = sourceTable?.columns.find(
                                (c) => c.id === sourceColId
                            );
                            const targetCol = targetTable?.columns.find(
                                (c) => c.id === targetColId
                            );
                            const pkCount =
                                sourceTable?.columns.filter((col) => col.pk)
                                    .length || 0;
                            const relation =
                                pkCount >= 2
                                    ? "N:M"
                                    : sourceCol?.unique
                                    ? "1:1"
                                    : "1:N";
                            const style =
                                pkCount >= 2
                                    ? {
                                          stroke: "#a78bfa",
                                          strokeWidth: 2,
                                          strokeDasharray: "6 4",
                                      }
                                    : sourceCol?.unique
                                    ? { stroke: "#84cc16", strokeWidth: 2 }
                                    : sourceCol?.pk
                                    ? { stroke: "#f97316", strokeWidth: 2 }
                                    : { stroke: "#06b6d4", strokeWidth: 2 };
                            const edgeId = `${source}-${sourceColId}`;
                            const edge = {
                                id: edgeId,
                                source,
                                sourceHandle,
                                target,
                                targetHandle,
                                label: `FK: ${
                                    sourceCol?.name || sourceColId
                                } → ${targetCol?.name || targetColId}`,
                                labelStyle: {
                                    fontSize: 12,
                                    fill: "#ffffff",
                                    fontWeight: 700,
                                },
                                labelBgStyle: {
                                    fill: "#0f172a",
                                    stroke: "rgba(255,255,255,0.05)",
                                    strokeWidth: 0,
                                },
                                labelBgPadding: [8, 6],
                                labelBgBorderRadius: 4,
                                type: "smoothstep",
                                style,
                                data: { relation },
                            };
                            setEdges((eds) => addEdge(edge, eds));
                            // parsed above; reuse existing sourceColId and targetColId
                            setTables((prev) =>
                                prev.map((t) => {
                                    if (t.id === source) {
                                        return {
                                            ...t,
                                            columns: t.columns.map((col) =>
                                                col.id === sourceColId
                                                    ? {
                                                          ...col,
                                                          fk: {
                                                              tableId: target,
                                                              colId: targetColId,
                                                          },
                                                      }
                                                    : col
                                            ),
                                        };
                                    }
                                    return t;
                                })
                            );
                        }}
                        nodeTypes={nodeTypes}
                        fitView
                        onInit={(rfi) => (reactFlowRef.current = rfi)}
                        onNodeClick={(_, node) => setSelectedTableId(node.id)}
                        onNodeDragStop={(e, node) => {
                            // sync moved node position back to tables
                            setTables((prev) =>
                                prev.map((t) =>
                                    t.id === node.id
                                        ? {
                                              ...t,
                                              x: node.position.x,
                                              y: node.position.y,
                                          }
                                        : t
                                )
                            );
                        }}
                        onPaneClick={() => setSelectedTableId(null)}
                        minZoom={0.2}
                        maxZoom={3}
                    >
                        <Background color="#3f3f46" gap={20} />
                        <Controls />
                        {/* MiniMap removed as requested */}
                    </ReactFlow>
                </div>
            </div>

            {/* --- Dialogs --- */}
            {deleteConfirm.show && (
                <div className="absolute inset-0 bg-black/80 backdrop-blur-sm z-[60] flex items-center justify-center p-4">
                    <div className="bg-zinc-900 border border-zinc-700 rounded-xl shadow-2xl w-full max-w-sm overflow-hidden animate-in fade-in zoom-in-95 duration-200">
                        <div className="p-5 flex flex-col items-center text-center">
                            <div className="w-12 h-12 bg-red-500/10 rounded-full flex items-center justify-center mb-4">
                                <AlertTriangle
                                    size={24}
                                    className="text-red-500"
                                />
                            </div>
                            <h3 className="font-bold text-lg text-zinc-100 mb-2">
                                Delete Entity?
                            </h3>
                            <p className="text-zinc-400 text-sm leading-relaxed">
                                Are you sure you want to delete{" "}
                                <span className="text-zinc-200 font-mono font-medium">
                                    {
                                        tables.find(
                                            (t) =>
                                                t.id === deleteConfirm.tableId
                                        )?.name
                                    }
                                </span>
                                ?
                                <br />
                                This will also remove component links.
                            </p>
                        </div>
                        <div className="bg-zinc-950/50 p-3 flex gap-3">
                            <button
                                onClick={() =>
                                    setDeleteConfirm({
                                        show: false,
                                        tableId: null,
                                    })
                                }
                                className="flex-1 px-4 py-2 bg-zinc-800 hover:bg-zinc-700 border border-zinc-600 rounded-lg text-sm font-medium"
                            >
                                Cancel
                            </button>
                            <button
                                onClick={confirmDeleteTable}
                                className="flex-1 px-4 py-2 bg-red-600 hover:bg-red-500 text-white rounded-lg text-sm font-medium shadow-lg shadow-red-900/20"
                            >
                                Delete
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {showSql && (
                <div className="absolute inset-0 bg-black/80 backdrop-blur-sm z-50 flex items-center justify-center p-4">
                    <div className="bg-zinc-900 border border-zinc-700 rounded-xl shadow-2xl w-full max-w-2xl flex flex-col max-h-[80vh] animate-in fade-in zoom-in-95 duration-200">
                        <div className="p-4 border-b border-zinc-800 flex items-center justify-between">
                            <h3 className="font-bold text-lg flex items-center gap-2 text-fuchsia-400">
                                <Gamepad2 size={20} /> Generated SQL
                            </h3>
                            <button
                                onClick={() => setShowSql(false)}
                                className="text-zinc-400 hover:text-white"
                            >
                                <X size={20} />
                            </button>
                        </div>
                        <div className="flex-1 overflow-auto p-0">
                            <pre className="p-4 text-xs sm:text-sm font-mono text-fuchsia-100 leading-relaxed">
                                {generateSQL()}
                            </pre>
                        </div>
                        <div className="p-4 border-t border-zinc-800 bg-zinc-950/50 flex justify-end gap-3">
                            <button
                                onClick={() =>
                                    navigator.clipboard.writeText(generateSQL())
                                }
                                className="flex items-center gap-2 px-4 py-2 bg-zinc-800 hover:bg-zinc-700 border border-zinc-600 rounded text-sm font-medium"
                            >
                                <Copy size={16} /> Copy
                            </button>
                            <button
                                onClick={() => {
                                    const blob = new Blob([generateSQL()], {
                                        type: "text/sql",
                                    });
                                    const url = URL.createObjectURL(blob);
                                    const a = document.createElement("a");
                                    a.href = url;
                                    a.download = "schema.sql";
                                    a.click();
                                }}
                                className="flex items-center gap-2 px-4 py-2 bg-fuchsia-600 hover:bg-fuchsia-500 text-white rounded text-sm font-medium shadow-lg"
                            >
                                <Download size={16} /> Download .sql
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};

const App = () => (
    <ReactFlowProvider>
        <AppContent />
    </ReactFlowProvider>
);

export default App;
