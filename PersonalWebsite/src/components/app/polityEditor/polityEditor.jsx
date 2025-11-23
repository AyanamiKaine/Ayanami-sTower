import React, { useState, useMemo } from "react";
import {
    Landmark,
    Briefcase,
    Globe,
    Save,
    Upload,
    Trash2,
    Plus,
    Settings,
    Download,
    AlertCircle,
    Flag,
    Scale,
    Scroll,
    Crown,
    Network,
    Shield,
    Users,
    ChevronRight,
} from "lucide-react";

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
        primary: "bg-amber-600 hover:bg-amber-700 text-white",
        secondary: "bg-slate-700 hover:bg-slate-600 text-slate-200",
        danger: "bg-red-900/50 hover:bg-red-900/70 text-red-200 border border-red-800",
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

const Input = ({
    label,
    value,
    onChange,
    type = "text",
    placeholder = "",
    className = "",
}) => (
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
            placeholder={placeholder}
            className="bg-slate-900 border border-slate-700 rounded p-2 text-slate-100 focus:outline-none focus:border-amber-500 transition-colors"
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
            className="bg-slate-900 border border-slate-700 rounded p-2 text-slate-100 focus:outline-none focus:border-amber-500 transition-colors appearance-none"
        >
            {options.map((opt) => (
                <option key={opt.value} value={opt.value}>
                    {opt.label}
                </option>
            ))}
        </select>
    </div>
);

// --- Main Application ---

export default function PolityArchitect() {
    const [activeTab, setActiveTab] = useState("polities");
    const [notification, setNotification] = useState(null);
    const [searchQuery, setSearchQuery] = useState("");

    // --- Data Models ---

    // 1. Polity Types (The Class of the Entity)
    const [polityTypes, setPolityTypes] = useState([
        {
            id: "pt_empire",
            name: "Sovereign Empire",
            description: "A fully independent state controlling territory.",
            icon: "Globe",
        },
        {
            id: "pt_corp",
            name: "Megacorporation",
            description: "A commercial entity spanning multiple systems.",
            icon: "Briefcase",
        },
        {
            id: "pt_federation",
            name: "Federation",
            description:
                "A multi-state federation sharing sovereignty under a common charter.",
            icon: "Globe",
        },
        {
            id: "pt_theocracy",
            name: "Theocracy",
            description:
                "Ruled directly by religious authorities or bound by a divine mandate.",
            icon: "Scroll",
        },
        {
            id: "pt_rebels",
            name: "Rebel Faction",
            description:
                "An insurgent organization contesting established authority.",
            icon: "Flag",
        },
        {
            id: "pt_citystate",
            name: "City-State",
            description:
                "A single metropolitan polity with autonomy often built around a core city.",
            icon: "Landmark",
        },
        {
            id: "pt_ngo",
            name: "NGO / Order",
            description: "A non-governmental organization or religious order.",
            icon: "Scale",
        },
        {
            id: "pt_vassal",
            name: "Vassal State",
            description: "A state subservient to a higher power.",
            icon: "Flag",
        },
        {
            id: "pt_kabal",
            name: "Shadow Kabal",
            description: "A secretive group pulling strings from the dark.",
            icon: "Users",
        },
        {
            id: "pt_colony",
            name: "Colony",
            description:
                "A polity established as an outpost of another power with limited autonomy.",
            icon: "Globe",
        },
    ]);

    // 2. Civics / Ethics (Building Blocks of Ideology)
    const [civics, setCivics] = useState([
        {
            id: "civ_militarist",
            name: "Militarist",
            type: "Ethic",
            description: "Might makes right.",
            opposites: ["civ_pacifist"],
        },
        {
            id: "civ_pacifist",
            name: "Pacifist",
            type: "Ethic",
            description: "Violence is the last resort.",
            opposites: ["civ_militarist"],
        },
        {
            id: "civ_feudal",
            name: "Feudal Structure",
            type: "Civic",
            description: "Organized through oaths of loyalty.",
            opposites: [],
        },
        {
            id: "civ_ruthless",
            name: "Ruthless Profit",
            type: "Civic",
            description: "Profit above ethics.",
            opposites: ["civ_socialist"],
        },
        {
            id: "civ_technocracy",
            name: "Technocracy",
            type: "Civic",
            description: "Ruled by experts and scientists.",
            opposites: [],
        },
        {
            id: "civ_hive",
            name: "Hive Mind",
            type: "Civic",
            description: "A single consciousness.",
            opposites: [],
        },
        {
            id: "civ_socialist",
            name: "Socialist",
            type: "Civic",
            description:
                "Collective ownership and state-led distribution of resources.",
            opposites: ["civ_ruthless", "civ_plutocracy", "civ_corporate"],
        },
        {
            id: "civ_isolationist",
            name: "Isolationist",
            type: "Policy",
            description:
                "Minimizes external contact and foreign entanglements.",
        },
        {
            id: "civ_expansionist",
            name: "Expansionist",
            type: "Policy",
            description: "Prioritizes territorial expansion and colonization.",
        },
        {
            id: "civ_merchant",
            name: "Trader Guilds",
            type: "Civic",
            description:
                "The economy is dominated by merchant interests and guilds.",
        },
        {
            id: "civ_authoritarian",
            name: "Authoritarian",
            type: "Ethic",
            description:
                "Centralized power concentrated in a single authority or ruling class.",
        },
        {
            id: "civ_religious",
            name: "Religious Rule",
            type: "Civic",
            description: "Religious doctrine defines laws and governance.",
        },
        {
            id: "civ_plutocracy",
            name: "Plutocracy",
            type: "Civic",
            description:
                "Rule by the wealthy where economic power drives political influence.",
        },
        {
            id: "civ_democratic",
            name: "Democratic",
            type: "Civic",
            description:
                "Governance through elected representatives and popular participation.",
        },
        {
            id: "civ_corporate",
            name: "Corporate Rule",
            type: "Civic",
            description:
                "Corporations control major aspects of society and governance.",
        },
    ]);

    // 3. Polities (The Actual Entities)
    const [polities, setPolities] = useState([
        {
            id: "pol_terran",
            name: "Terran Dominion",
            typeId: "pt_empire",
            parentId: "",
            color: "#3b82f6",
            leaderTitle: "High Praetor",
            civics: ["civ_militarist", "civ_feudal"],
        },
        {
            id: "pol_mining",
            name: "Orion Heavy Industries",
            typeId: "pt_corp",
            parentId: "pol_terran", // Subsidiary/Operating within Terran space
            color: "#f59e0b",
            leaderTitle: "CEO",
            civics: ["civ_ruthless"],
        },
        {
            id: "pol_cult",
            name: "Order of the Void",
            typeId: "pt_kabal",
            parentId: "",
            color: "#7c3aed",
            leaderTitle: "Grand Master",
            civics: ["civ_technocracy"],
        },
    ]);

    // --- Helpers ---
    const showNotification = (msg, type = "success") => {
        setNotification({ msg, type });
        setTimeout(() => setNotification(null), 3000);
    };

    const generateId = (prefix) =>
        `${prefix}_${Math.random().toString(36).substr(2, 9)}`;

    // --- Handlers: Polities ---
    const addPolity = () => {
        setPolities([
            ...polities,
            {
                id: generateId("pol"),
                name: "New Polity",
                typeId: polityTypes[0].id,
                parentId: "",
                color: "#ffffff",
                leaderTitle: "Leader",
                civics: [],
            },
        ]);
    };

    const updatePolity = (id, field, value) => {
        setPolities(
            polities.map((p) => (p.id === id ? { ...p, [field]: value } : p))
        );
    };

    const togglePolityCivic = (polityId, civicId) => {
        setPolities(
            polities.map((p) => {
                if (p.id !== polityId) return p;
                const hasCivic = p.civics.includes(civicId);
                // If adding civic, ensure it doesn't conflict with existing civics
                if (!hasCivic) {
                    const civicDef = civics.find((c) => c.id === civicId);
                    if (
                        civicDef &&
                        civicDef.opposites &&
                        civicDef.opposites.length > 0
                    ) {
                        const conflicting = p.civics.find((c) =>
                            civicDef.opposites.includes(c)
                        );
                        if (conflicting) {
                            const conflictName =
                                civics.find((c) => c.id === conflicting)
                                    ?.name || conflicting;
                            return (
                                showNotification(
                                    `Can't add ${civicDef.name}: conflicts with ${conflictName}`,
                                    "error"
                                ) || p
                            ); // prevent change
                        }
                    }
                    // Also ensure not adding civic that is opposite of any existing selected civics
                    const oppositeConflict = p.civics.find((existingId) => {
                        const existing = civics.find(
                            (c) => c.id === existingId
                        );
                        return existing?.opposites?.includes(civicId);
                    });
                    if (oppositeConflict) {
                        const conflictName =
                            civics.find((c) => c.id === oppositeConflict)
                                ?.name || oppositeConflict;
                        return (
                            showNotification(
                                `Can't add ${
                                    civics.find((c) => c.id === civicId)?.name
                                }: conflicts with ${conflictName}`,
                                "error"
                            ) || p
                        );
                    }
                }
                return {
                    ...p,
                    civics: hasCivic
                        ? p.civics.filter((c) => c !== civicId)
                        : [...p.civics, civicId],
                };
            })
        );
    };

    // Toggle reciprocal opposites for civics
    const toggleCivicOpposite = (civicId, otherCivicId) => {
        setCivics((prev) => {
            const toggled = prev.map((c) => {
                if (c.id !== civicId) return c;
                const isPresent = (c.opposites || []).includes(otherCivicId);
                return {
                    ...c,
                    opposites: isPresent
                        ? c.opposites.filter((o) => o !== otherCivicId)
                        : [...(c.opposites || []), otherCivicId],
                };
            });

            // Ensure reciprocal change
            return toggled.map((c) => {
                if (c.id !== otherCivicId) return c;
                const primaryHasOpposite = toggled
                    .find((n) => n.id === civicId)
                    .opposites.includes(otherCivicId);
                const isPresent = (c.opposites || []).includes(civicId);
                if (primaryHasOpposite && !isPresent) {
                    return {
                        ...c,
                        opposites: [...(c.opposites || []), civicId],
                    };
                }
                if (!primaryHasOpposite && isPresent) {
                    return {
                        ...c,
                        opposites: c.opposites.filter((o) => o !== civicId),
                    };
                }
                return c;
            });
        });
    };

    const deletePolity = (id) => {
        if (confirm("Delete this polity?")) {
            setPolities(polities.filter((p) => p.id !== id));
            // Clean up children references if needed, or just let them be orphaned
        }
    };

    // Special handler to change a polity id and update references
    const updatePolityId = (oldId, newId) => {
        newId = (newId || "").trim();
        if (!newId) return showNotification("ID cannot be empty", "error");
        if (newId === oldId) return;
        // Prevent duplicates
        if (polities.some((p) => p.id === newId))
            return showNotification(
                "A polity with that ID already exists",
                "error"
            );

        setPolities(
            polities.map((p) => {
                if (p.id === oldId) return { ...p, id: newId };
                if (p.parentId === oldId) return { ...p, parentId: newId };
                return p;
            })
        );
    };

    // --- Handlers: Types & Civics ---
    const addType = () => {
        setPolityTypes([
            ...polityTypes,
            {
                id: generateId("pt"),
                name: "New Type",
                description: "Description...",
                icon: "Globe",
            },
        ]);
    };
    const updateType = (id, f, v) =>
        setPolityTypes(
            polityTypes.map((t) => (t.id === id ? { ...t, [f]: v } : t))
        );
    const deleteType = (id) =>
        setPolityTypes(polityTypes.filter((t) => t.id !== id));

    const addCivic = () => {
        setCivics([
            ...civics,
            {
                id: generateId("civ"),
                name: "New Civic",
                type: "Civic",
                description: "Effect...",
                opposites: [],
            },
        ]);
    };
    const updateCivic = (id, f, v) =>
        setCivics(civics.map((c) => (c.id === id ? { ...c, [f]: v } : c)));
    const deleteCivic = (id) => deleteCivicWithCleanup(id);

    // delete civic with cleanup: remove references from polities and from other civics' opposites
    const deleteCivicWithCleanup = (id) => {
        if (
            !confirm(
                "Delete this civic? This will remove it from all polities and opposing lists."
            )
        )
            return;
        setCivics((prev) =>
            prev
                .filter((c) => c.id !== id)
                .map((c) => ({
                    ...c,
                    opposites: c.opposites.filter((o) => o !== id),
                }))
        );
        setPolities((prev) =>
            prev.map((p) => ({
                ...p,
                civics: p.civics.filter((cid) => cid !== id),
            }))
        );
    };

    // --- Handlers: Data ---
    const exportData = () => {
        const data = JSON.stringify({ polities, polityTypes, civics }, null, 2);
        const blob = new Blob([data], { type: "application/json" });
        const url = URL.createObjectURL(blob);
        const a = document.createElement("a");
        a.href = url;
        a.download = "stella_invicta_polities.json";
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
    };

    const importData = (e) => {
        const file = e.target.files[0];
        if (!file) return;
        const reader = new FileReader();
        reader.onload = (event) => {
            try {
                const json = JSON.parse(event.target.result);
                if (json.polities) setPolities(json.polities);
                if (json.polityTypes) setPolityTypes(json.polityTypes);
                if (json.civics) setCivics(json.civics);
                showNotification("Polity Database Loaded");
            } catch (err) {
                showNotification("Error parsing JSON", "error");
            }
        };
        reader.readAsText(file);
    };

    // --- Renderers ---

    const filteredPolities = useMemo(() => {
        if (!searchQuery) return polities;
        return polities.filter((p) =>
            p.name.toLowerCase().includes(searchQuery.toLowerCase())
        );
    }, [polities, searchQuery]);

    const renderPolities = () => (
        <div className="space-y-6">
            <div className="flex justify-between items-center">
                <div>
                    <h2 className="text-2xl font-bold text-white">
                        Polity Registry
                    </h2>
                    <p className="text-slate-400">
                        Define nations, corporations, and organizations.
                    </p>
                </div>
                <div className="flex gap-2">
                    <Input
                        placeholder="Search..."
                        value={searchQuery}
                        onChange={(e) => setSearchQuery(e.target.value)}
                        className="w-48"
                    />
                    <Button onClick={addPolity}>
                        <Plus size={16} /> Create New
                    </Button>
                </div>
            </div>

            <div className="grid grid-cols-1 gap-4">
                {filteredPolities.map((polity) => {
                    const pType = polityTypes.find(
                        (t) => t.id === polity.typeId
                    );
                    const parent = polities.find(
                        (p) => p.id === polity.parentId
                    );

                    return (
                        <Card
                            key={polity.id}
                            className="p-4 flex flex-col lg:flex-row gap-6 relative overflow-hidden group"
                        >
                            {/* Color Strip */}
                            <div
                                className="absolute left-0 top-0 bottom-0 w-2"
                                style={{ backgroundColor: polity.color }}
                            />

                            {/* Left: Core Info */}
                            <div className="flex-1 space-y-4 pl-4">
                                <div className="flex justify-between items-start">
                                    <div className="flex items-center gap-3">
                                        <div className="p-3 bg-slate-900 rounded-lg border border-slate-700 text-amber-500">
                                            <Landmark size={24} />
                                        </div>
                                        <div>
                                            <Input
                                                value={polity.name}
                                                onChange={(e) =>
                                                    updatePolity(
                                                        polity.id,
                                                        "name",
                                                        e.target.value
                                                    )
                                                }
                                                className="font-bold text-lg w-full md:w-72"
                                            />
                                            <div className="flex gap-2 mt-1 items-center text-xs text-slate-400">
                                                <input
                                                    defaultValue={polity.id}
                                                    onBlur={(e) =>
                                                        updatePolityId(
                                                            polity.id,
                                                            e.target.value
                                                        )
                                                    }
                                                    onKeyDown={(e) => {
                                                        if (e.key === "Enter")
                                                            e.target.blur();
                                                    }}
                                                    className="bg-transparent font-mono opacity-70 border-none text-xs text-slate-400 focus:outline-none"
                                                    aria-label={`Edit ID for ${polity.name}`}
                                                />
                                                {parent && (
                                                    <span className="flex items-center gap-1 bg-slate-900 px-2 py-0.5 rounded text-blue-300 border border-slate-700">
                                                        <ChevronRight
                                                            size={10}
                                                        />{" "}
                                                        Sub-polity of{" "}
                                                        {parent.name}
                                                    </span>
                                                )}
                                            </div>
                                        </div>
                                    </div>
                                    <button
                                        onClick={() => deletePolity(polity.id)}
                                        className="text-slate-600 hover:text-red-400"
                                    >
                                        <Trash2 size={16} />
                                    </button>
                                </div>

                                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                    <Select
                                        label="Polity Type"
                                        value={polity.typeId}
                                        onChange={(e) =>
                                            updatePolity(
                                                polity.id,
                                                "typeId",
                                                e.target.value
                                            )
                                        }
                                        options={polityTypes.map((t) => ({
                                            value: t.id,
                                            label: t.name,
                                        }))}
                                    />
                                    <Select
                                        label="Parent Entity (Optional)"
                                        value={polity.parentId}
                                        onChange={(e) =>
                                            updatePolity(
                                                polity.id,
                                                "parentId",
                                                e.target.value
                                            )
                                        }
                                        options={[
                                            {
                                                value: "",
                                                label: "-- Independent --",
                                            },
                                            ...polities
                                                .filter(
                                                    (p) => p.id !== polity.id
                                                )
                                                .map((p) => ({
                                                    value: p.id,
                                                    label: p.name,
                                                })),
                                        ]}
                                    />
                                    <Input
                                        label="Leader Title"
                                        value={polity.leaderTitle}
                                        onChange={(e) =>
                                            updatePolity(
                                                polity.id,
                                                "leaderTitle",
                                                e.target.value
                                            )
                                        }
                                    />
                                    <div className="flex flex-col gap-1">
                                        <label className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
                                            Map Color
                                        </label>
                                        <div className="flex items-center gap-2">
                                            <input
                                                type="color"
                                                value={polity.color}
                                                onChange={(e) =>
                                                    updatePolity(
                                                        polity.id,
                                                        "color",
                                                        e.target.value
                                                    )
                                                }
                                                className="bg-transparent h-9 w-16 cursor-pointer rounded border border-slate-700"
                                            />
                                            <span className="text-xs font-mono text-slate-500">
                                                {polity.color}
                                            </span>
                                        </div>
                                    </div>
                                </div>
                            </div>

                            {/* Right: Civics & Ethics */}
                            <div className="flex-1 bg-slate-900/30 border-l border-slate-700 pl-6 border-dashed p-2 rounded">
                                <div className="flex justify-between items-center mb-2">
                                    <label className="text-xs font-semibold text-slate-400 uppercase tracking-wider flex items-center gap-2">
                                        <Scale size={14} /> Civics & Ethics
                                    </label>
                                </div>

                                <div className="flex flex-wrap gap-2 mb-4">
                                    {polity.civics.map((cid) => {
                                        const civic = civics.find(
                                            (c) => c.id === cid
                                        );
                                        return civic ? (
                                            <span
                                                key={cid}
                                                className="px-2 py-1 rounded text-xs bg-slate-700 text-slate-200 border border-slate-600 flex items-center gap-1 cursor-help"
                                                title={civic.description}
                                            >
                                                {civic.name}
                                                <button
                                                    onClick={() =>
                                                        togglePolityCivic(
                                                            polity.id,
                                                            cid
                                                        )
                                                    }
                                                    className="hover:text-red-400 ml-1"
                                                >
                                                    <Trash2 size={10} />
                                                </button>
                                            </span>
                                        ) : null;
                                    })}
                                    {polity.civics.length === 0 && (
                                        <span className="text-slate-600 text-xs italic">
                                            No civics defined.
                                        </span>
                                    )}
                                </div>

                                <div className="border-t border-slate-700 pt-2">
                                    <label className="text-[10px] font-bold text-slate-500 uppercase mb-1 block">
                                        Available Civics
                                    </label>
                                    <div className="flex flex-wrap gap-1 max-h-32 overflow-y-auto custom-scrollbar">
                                        {civics
                                            .filter(
                                                (c) =>
                                                    !polity.civics.includes(
                                                        c.id
                                                    )
                                            )
                                            .map((c) => (
                                                <button
                                                    key={c.id}
                                                    onClick={() =>
                                                        togglePolityCivic(
                                                            polity.id,
                                                            c.id
                                                        )
                                                    }
                                                    className="px-2 py-1 rounded text-[10px] bg-slate-900 border border-slate-700 text-slate-400 hover:border-amber-500 hover:text-amber-500 transition-colors text-left"
                                                    title={c.description}
                                                >
                                                    + {c.name}
                                                </button>
                                            ))}
                                    </div>
                                </div>
                            </div>
                        </Card>
                    );
                })}
            </div>
        </div>
    );

    const renderTypes = () => (
        <div className="space-y-6">
            <div className="flex justify-between items-center">
                <div>
                    <h2 className="text-2xl font-bold text-white">
                        Polity Classifications
                    </h2>
                    <p className="text-slate-400">
                        Define the types of organizations that exist.
                    </p>
                </div>
                <Button onClick={addType}>
                    <Plus size={16} /> New Type
                </Button>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {polityTypes.map((type) => (
                    <Card key={type.id} className="p-4">
                        <div className="flex justify-between mb-2 items-center">
                            <div className="flex items-center gap-2">
                                <div className="p-1.5 bg-slate-700 rounded text-slate-300">
                                    <Globe size={16} />
                                </div>
                                <Input
                                    value={type.name}
                                    onChange={(e) =>
                                        updateType(
                                            type.id,
                                            "name",
                                            e.target.value
                                        )
                                    }
                                    className="font-bold"
                                />
                            </div>
                            <button
                                onClick={() => deleteType(type.id)}
                                className="text-slate-500 hover:text-red-400"
                            >
                                <Trash2 size={16} />
                            </button>
                        </div>
                        <Input
                            value={type.description}
                            onChange={(e) =>
                                updateType(
                                    type.id,
                                    "description",
                                    e.target.value
                                )
                            }
                            placeholder="Description..."
                        />
                    </Card>
                ))}
            </div>
        </div>
    );

    const renderCivics = () => (
        <div className="space-y-6">
            <div className="flex justify-between items-center">
                <div>
                    <h2 className="text-2xl font-bold text-white">
                        Civics & Ethics
                    </h2>
                    <p className="text-slate-400">
                        Policies, laws, and cultural values.
                    </p>
                </div>
                <Button onClick={addCivic}>
                    <Plus size={16} /> New Civic
                </Button>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                {civics.map((civic) => (
                    <Card key={civic.id} className="p-4">
                        <div className="flex justify-between mb-2 items-start">
                            <div className="flex-1">
                                <Input
                                    value={civic.name}
                                    onChange={(e) =>
                                        updateCivic(
                                            civic.id,
                                            "name",
                                            e.target.value
                                        )
                                    }
                                    className="font-bold mb-1"
                                />
                                <select
                                    value={civic.type}
                                    onChange={(e) =>
                                        updateCivic(
                                            civic.id,
                                            "type",
                                            e.target.value
                                        )
                                    }
                                    className="bg-slate-900 text-xs px-2 py-1 rounded border border-slate-700 text-amber-500"
                                >
                                    <option>Civic</option>
                                    <option>Ethic</option>
                                    <option>Origin</option>
                                </select>
                            </div>
                            <button
                                onClick={() => deleteCivic(civic.id)}
                                className="text-slate-500 hover:text-red-400"
                            >
                                <Trash2 size={16} />
                            </button>
                        </div>
                        <textarea
                            value={civic.description}
                            onChange={(e) =>
                                updateCivic(
                                    civic.id,
                                    "description",
                                    e.target.value
                                )
                            }
                            placeholder="Effect/Description..."
                            className="w-full bg-slate-900 border border-slate-700 rounded p-2 text-xs text-slate-300 focus:outline-none focus:border-amber-500 min-h-[60px]"
                        />

                        <div className="bg-slate-900/50 p-3 rounded border border-slate-700/50 mt-3">
                            <label className="text-xs font-semibold text-slate-400 uppercase tracking-wider mb-2 block">
                                Opposing Civics (Conflicts)
                            </label>
                            <div className="flex flex-wrap gap-2">
                                {civics
                                    .filter((other) => other.id !== civic.id)
                                    .map((other) => {
                                        const isOpp = civic.opposites?.includes(
                                            other.id
                                        );
                                        return (
                                            <button
                                                key={other.id}
                                                onClick={() =>
                                                    toggleCivicOpposite(
                                                        civic.id,
                                                        other.id
                                                    )
                                                }
                                                className={`px-2 py-1 rounded text-xs border transition-colors ${
                                                    isOpp
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
                                Civics marked as opposing cannot be selected
                                together for a Polity.
                            </p>
                        </div>
                    </Card>
                ))}
            </div>
        </div>
    );

    const renderData = () => (
        <div className="max-w-xl mx-auto p-6 bg-slate-800 rounded border border-slate-700 text-center space-y-6">
            <div>
                <h3 className="text-xl font-bold text-white mb-2">
                    Data Management
                </h3>
                <p className="text-slate-400 text-sm">
                    Import or export your Polity database for use in the game
                    engine.
                </p>
            </div>
            <div className="flex gap-4 justify-center">
                <Button onClick={exportData}>
                    <Download size={16} /> Export JSON
                </Button>
                <label className="cursor-pointer bg-slate-700 text-slate-200 px-4 py-2 rounded flex items-center gap-2 hover:bg-slate-600 text-sm font-medium transition-colors">
                    <Upload size={16} /> Import JSON
                    <input
                        type="file"
                        className="hidden"
                        accept=".json"
                        onChange={importData}
                    />
                </label>
            </div>
            <div className="bg-slate-950 p-4 rounded-lg border border-slate-800 font-mono text-xs text-slate-300 overflow-auto max-h-64 text-left">
                <pre>
                    {JSON.stringify({ polities, polityTypes, civics }, null, 2)}
                </pre>
            </div>
        </div>
    );

    return (
        <div className="min-h-screen bg-slate-950 text-slate-200 font-sans selection:bg-amber-500/30">
            {/* Header */}
            <header className="bg-slate-900 border-b border-slate-800 sticky top-0 z-10">
                <div className="max-w-7xl mx-auto px-4 h-16 flex items-center justify-between">
                    <div className="flex items-center gap-3">
                        <div className="w-8 h-8 bg-gradient-to-br from-amber-500 to-orange-600 rounded-lg flex items-center justify-center shadow-lg shadow-amber-500/20">
                            <Crown className="text-white" size={18} />
                        </div>
                        <h1 className="font-bold text-xl tracking-tight text-white">
                            Stella Invicta{" "}
                            <span className="text-slate-500 font-normal">
                                | Polity Architect
                            </span>
                        </h1>
                    </div>

                    <div className="flex items-center gap-1 bg-slate-800 p-1 rounded-lg overflow-x-auto">
                        {[
                            {
                                id: "polities",
                                label: "Polities",
                                icon: Landmark,
                            },
                            { id: "types", label: "Types", icon: Globe },
                            { id: "civics", label: "Civics", icon: Scale },
                            { id: "data", label: "Data", icon: Save },
                        ].map((tab) => (
                            <button
                                key={tab.id}
                                onClick={() => setActiveTab(tab.id)}
                                className={`px-3 py-2 rounded-md text-sm font-medium flex items-center gap-2 transition-all whitespace-nowrap ${
                                    activeTab === tab.id
                                        ? "bg-slate-700 text-white shadow-sm"
                                        : "text-slate-400 hover:text-slate-200 hover:bg-slate-800"
                                }`}
                            >
                                <tab.icon size={16} />
                                <span className="hidden sm:inline">
                                    {tab.label}
                                </span>
                            </button>
                        ))}
                    </div>
                </div>
            </header>

            {/* Main Content */}
            <main className="max-w-7xl mx-auto px-4 py-8">
                {notification && (
                    <div
                        className={`fixed bottom-6 right-6 px-6 py-3 rounded-lg shadow-xl border flex items-center gap-3 animate-fade-in-up z-50 ${
                            notification.type === "error"
                                ? "bg-red-900/90 border-red-700 text-white"
                                : "bg-emerald-900/90 border-emerald-700 text-white"
                        }`}
                    >
                        {notification.type === "error" ? (
                            <AlertCircle size={20} />
                        ) : (
                            <Save size={20} />
                        )}
                        {notification.msg}
                    </div>
                )}

                <div className="animate-in fade-in slide-in-from-bottom-4 duration-300">
                    {activeTab === "polities" && renderPolities()}
                    {activeTab === "types" && renderTypes()}
                    {activeTab === "civics" && renderCivics()}
                    {activeTab === "data" && renderData()}
                </div>
            </main>
        </div>
    );
}
