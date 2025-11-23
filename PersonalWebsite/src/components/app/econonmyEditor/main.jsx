import React, { useState, useEffect } from "react";
import {
    Package,
    Factory,
    Users,
    Save,
    Upload,
    Trash2,
    Plus,
    ChevronRight,
    ChevronDown,
    ArrowRight,
    Settings,
    Download,
    AlertCircle,
    Database,
    Dna,
    Globe,
    BookOpen,
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
        primary: "bg-blue-600 hover:bg-blue-700 text-white",
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
            className="bg-slate-900 border border-slate-700 rounded p-2 text-slate-100 focus:outline-none focus:border-blue-500 transition-colors"
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
            className="bg-slate-900 border border-slate-700 rounded p-2 text-slate-100 focus:outline-none focus:border-blue-500 transition-colors appearance-none"
        >
            {options.map((opt) => (
                <option key={opt.value} value={opt.value}>
                    {opt.label}
                </option>
            ))}
        </select>
    </div>
);

// --- Reusable Logic ---

// Helper to generate a generic structure for any entity with needs
const createEntity = (prefix, name) => ({
    id: `${prefix}_${Math.random().toString(36).substr(2, 9)}`,
    name,
    needs: { life: [], everyday: [], luxury: [] },
});

// --- Main Application ---

export default function EconomicArchitect() {
    // --- State ---
    const [activeTab, setActiveTab] = useState("resources");
    const [notification, setNotification] = useState(null);

    // 1. Resources (loaded from defaults)
    const [resources, setResources] = useState([]);

    // 2. Production (loaded from defaults)
    const [recipes, setRecipes] = useState([]);

    // 3. Social Classes (Job Types) (loaded from defaults)
    const [popTypes, setPopTypes] = useState([]);

    // 4. Species (Biological Needs) (loaded from defaults)
    const [species, setSpecies] = useState([]);

    // 5. Cultures (Social/Cultural Preferences) (loaded from defaults)
    const [cultures, setCultures] = useState([]);

    // 6. Religions (Spiritual Needs) (loaded from defaults)
    const [religions, setReligions] = useState([]);

    // Load defaults from public JSON
    useEffect(() => {
        const url = "/defaults/economy-defaults.json";
        fetch(url)
            .then((res) => {
                if (!res.ok)
                    throw new Error("Failed to fetch economy defaults");
                return res.json();
            })
            .then((data) => {
                if (data.resources) setResources(data.resources);
                if (data.recipes) setRecipes(data.recipes);
                if (data.popTypes) setPopTypes(data.popTypes);
                if (data.species) setSpecies(data.species);
                if (data.cultures) setCultures(data.cultures);
                if (data.religions) setReligions(data.religions);
            })
            .catch((err) => {
                console.warn("economy defaults fetch error", err);
            });
    }, []);

    // --- Helpers ---
    const showNotification = (msg, type = "success") => {
        setNotification({ msg, type });
        setTimeout(() => setNotification(null), 3000);
    };

    const generateId = (prefix) =>
        `${prefix}_${Math.random().toString(36).substr(2, 9)}`;

    // --- Generic Handlers for Needs-Based Entities ---

    // Generic Add
    const addEntity = (setFunction, list, prefix, defaultName) => {
        setFunction([...list, createEntity(prefix, defaultName)]);
    };

    // Generic Update Name
    const updateEntityName = (setFunction, list, id, name) => {
        setFunction(
            list.map((item) => (item.id === id ? { ...item, name } : item))
        );
    };

    // Generic Delete
    const deleteEntity = (setFunction, list, id) => {
        if (confirm("Are you sure you want to delete this entry?")) {
            setFunction(list.filter((item) => item.id !== id));
        }
    };

    // Generic Update Need
    const updateNeed = (
        setFunction,
        list,
        entityId,
        category,
        index,
        field,
        value
    ) => {
        setFunction(
            list.map((entity) => {
                if (entity.id !== entityId) return entity;
                const newNeeds = { ...entity.needs };
                newNeeds[category][index] = {
                    ...newNeeds[category][index],
                    [field]: value,
                };
                return { ...entity, needs: newNeeds };
            })
        );
    };

    // Generic Add Need
    const addNeed = (setFunction, list, entityId, category) => {
        setFunction(
            list.map((entity) => {
                if (entity.id !== entityId) return entity;
                const defaultResId = resources[0]?.id || "";
                const newNeeds = { ...entity.needs };
                newNeeds[category] = [
                    ...newNeeds[category],
                    { resourceId: defaultResId, amount: 1 },
                ];
                return { ...entity, needs: newNeeds };
            })
        );
    };

    // Generic Remove Need
    const removeNeed = (setFunction, list, entityId, category, index) => {
        setFunction(
            list.map((entity) => {
                if (entity.id !== entityId) return entity;
                const newNeeds = { ...entity.needs };
                newNeeds[category].splice(index, 1);
                return { ...entity, needs: newNeeds };
            })
        );
    };

    // --- Specific Resource & Recipe Handlers (Not generic) ---
    const addResource = () => {
        setResources([
            ...resources,
            {
                id: generateId("res"),
                name: "New Resource",
                category: "Raw Material",
            },
        ]);
    };

    const updateResource = (id, field, value) => {
        setResources(
            resources.map((r) => (r.id === id ? { ...r, [field]: value } : r))
        );
    };

    const deleteResource = (id) => {
        if (confirm("Delete this resource?"))
            setResources(resources.filter((r) => r.id !== id));
    };

    const addRecipe = () => {
        setRecipes([
            ...recipes,
            {
                id: generateId("rec"),
                name: "New Method",
                inputs: [],
                outputs: [],
                workforce: [],
            },
        ]);
    };

    // Recipe sub-handlers (omitted for brevity, assume similar to previous version but using state directly)
    // Re-implementing explicitly to ensure they work
    const updateRecipeName = (id, val) =>
        setRecipes(recipes.map((r) => (r.id === id ? { ...r, name: val } : r)));

    const modifyRecipeList = (id, type, action, index, field, val) => {
        setRecipes(
            recipes.map((r) => {
                if (r.id !== id) return r;
                const list = [...r[type]];
                if (action === "add")
                    list.push({ resourceId: resources[0]?.id, amount: 1 });
                if (action === "remove") list.splice(index, 1);
                if (action === "update") list[index][field] = val;
                return { ...r, [type]: list };
            })
        );
    };

    const modifyRecipeWorkforce = (id, action, index, field, val) => {
        setRecipes(
            recipes.map((r) => {
                if (r.id !== id) return r;
                const list = [...(r.workforce || [])];
                if (action === "add")
                    list.push({ popId: popTypes[0]?.id, amount: 100 });
                if (action === "remove") list.splice(index, 1);
                if (action === "update") list[index][field] = val;
                return { ...r, workforce: list };
            })
        );
    };

    // --- Handlers: Data ---
    const exportData = () => {
        const data = JSON.stringify(
            { resources, recipes, popTypes, species, cultures, religions },
            null,
            2
        );
        const blob = new Blob([data], { type: "application/json" });
        const url = URL.createObjectURL(blob);
        const a = document.createElement("a");
        a.href = url;
        a.download = "stella_invicta_economy.json";
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
        showNotification("Full Database Exported");
    };

    const importData = (e) => {
        const file = e.target.files[0];
        if (!file) return;
        const reader = new FileReader();
        reader.onload = (event) => {
            try {
                const json = JSON.parse(event.target.result);
                if (json.resources) setResources(json.resources);
                if (json.recipes) setRecipes(json.recipes);
                if (json.popTypes) setPopTypes(json.popTypes);
                if (json.species) setSpecies(json.species);
                if (json.cultures) setCultures(json.cultures);
                if (json.religions) setReligions(json.religions);
                showNotification("Database Imported Successfully");
            } catch (err) {
                showNotification("Failed to parse JSON", "error");
            }
        };
        reader.readAsText(file);
    };

    const copyToClipboard = () => {
        const data = JSON.stringify(
            { resources, recipes, popTypes, species, cultures, religions },
            null,
            2
        );
        navigator.clipboard
            .writeText(data)
            .then(() => showNotification("Copied JSON to clipboard"));
    };

    // --- UI Components ---

    // Reusable Component for any entity that has Needs (Pop Class, Species, Culture, Religion)
    const NeedsEditor = ({
        entity,
        setFunction,
        list,
        colorTheme = "blue",
        icon: Icon,
    }) => {
        const renderCategory = (category, title, colorText) => (
            <div className="mb-4">
                <div
                    className={`flex justify-between items-center mb-2 pb-1 border-b border-slate-700 ${colorText}`}
                >
                    <h4 className="text-sm font-bold uppercase">{title}</h4>
                    <button
                        onClick={() =>
                            addNeed(setFunction, list, entity.id, category)
                        }
                        className="text-xs flex items-center gap-1 opacity-70 hover:opacity-100 transition-opacity"
                    >
                        <Plus size={12} /> Add
                    </button>
                </div>
                <div className="space-y-2">
                    {entity.needs[category].length === 0 && (
                        <p className="text-xs text-slate-600 italic">
                            No modifiers defined.
                        </p>
                    )}
                    {entity.needs[category].map((need, idx) => (
                        <div
                            key={idx}
                            className="flex items-center gap-2 text-sm animate-in fade-in duration-300"
                        >
                            <span className="text-slate-400 text-xs w-14">
                                Demands
                            </span>
                            <input
                                type="number"
                                value={need.amount}
                                onChange={(e) =>
                                    updateNeed(
                                        setFunction,
                                        list,
                                        entity.id,
                                        category,
                                        idx,
                                        "amount",
                                        parseFloat(e.target.value)
                                    )
                                }
                                className="w-16 bg-slate-900 border border-slate-700 rounded px-2 py-1 text-slate-200 text-xs"
                            />
                            <select
                                value={need.resourceId}
                                onChange={(e) =>
                                    updateNeed(
                                        setFunction,
                                        list,
                                        entity.id,
                                        category,
                                        idx,
                                        "resourceId",
                                        e.target.value
                                    )
                                }
                                className="flex-1 bg-slate-900 border border-slate-700 rounded px-2 py-1 text-slate-200 text-xs"
                            >
                                {resources.map((r) => (
                                    <option
                                        key={r.id}
                                        value={r.id}
                                        className="bg-slate-900"
                                    >
                                        {r.name}
                                    </option>
                                ))}
                            </select>
                            <button
                                onClick={() =>
                                    removeNeed(
                                        setFunction,
                                        list,
                                        entity.id,
                                        category,
                                        idx
                                    )
                                }
                                className="text-slate-600 hover:text-red-400"
                            >
                                <Trash2 size={12} />
                            </button>
                        </div>
                    ))}
                </div>
            </div>
        );

        return (
            <Card className="p-4 flex flex-col h-full hover:border-slate-600 transition-colors">
                <div className="flex justify-between items-start mb-4">
                    <div className="flex items-center gap-3 flex-1">
                        <div
                            className={`p-2 bg-${colorTheme}-900/30 rounded-lg text-${colorTheme}-400`}
                        >
                            <Icon size={20} />
                        </div>
                        <Input
                            value={entity.name}
                            onChange={(e) =>
                                updateEntityName(
                                    setFunction,
                                    list,
                                    entity.id,
                                    e.target.value
                                )
                            }
                            className="w-full max-w-[200px]"
                        />
                    </div>
                    <button
                        onClick={() =>
                            deleteEntity(setFunction, list, entity.id)
                        }
                        className="text-slate-500 hover:text-red-400 transition-colors"
                    >
                        <Trash2 size={16} />
                    </button>
                </div>

                <div className="flex-1 space-y-2 bg-slate-900/30 p-3 rounded-lg border border-slate-700/30">
                    {renderCategory("life", "Life Needs", "text-red-400")}
                    {renderCategory(
                        "everyday",
                        "Everyday Needs",
                        "text-blue-400"
                    )}
                    {renderCategory("luxury", "Luxury Needs", "text-amber-400")}
                </div>
            </Card>
        );
    };

    // --- Main Render Functions ---

    const renderResources = () => (
        <div className="space-y-4">
            <div className="flex justify-between items-center mb-6">
                <div>
                    <h2 className="text-2xl font-bold text-white">
                        Resource Registry
                    </h2>
                    <p className="text-slate-400">
                        Base materials and manufactured goods.
                    </p>
                </div>
                <Button onClick={addResource}>
                    <Plus size={16} /> New Resource
                </Button>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                {resources.map((res) => (
                    <Card key={res.id} className="p-4">
                        <div className="flex justify-between items-start mb-3">
                            <div className="p-2 bg-blue-900/30 rounded-lg text-blue-400">
                                <Package size={20} />
                            </div>
                            <button
                                onClick={() => deleteResource(res.id)}
                                className="text-slate-500 hover:text-red-400"
                            >
                                <Trash2 size={16} />
                            </button>
                        </div>
                        <div className="space-y-3">
                            <Input
                                label="Name"
                                value={res.name}
                                onChange={(e) =>
                                    updateResource(
                                        res.id,
                                        "name",
                                        e.target.value
                                    )
                                }
                            />
                            <Input
                                label="Category"
                                value={res.category}
                                onChange={(e) =>
                                    updateResource(
                                        res.id,
                                        "category",
                                        e.target.value
                                    )
                                }
                            />
                            <div className="text-xs text-slate-500 font-mono pt-2 border-t border-slate-700">
                                ID: {res.id}
                            </div>
                        </div>
                    </Card>
                ))}
            </div>
        </div>
    );

    const renderProduction = () => (
        <div className="space-y-4">
            <div className="flex justify-between items-center mb-6">
                <div>
                    <h2 className="text-2xl font-bold text-white">
                        Production Chains
                    </h2>
                    <p className="text-slate-400">
                        Recipes and industrial processes.
                    </p>
                </div>
                <Button onClick={addRecipe}>
                    <Plus size={16} /> New Chain
                </Button>
            </div>
            <div className="space-y-4">
                {recipes.map((recipe) => (
                    <Card key={recipe.id} className="p-4">
                        <div className="flex items-center justify-between mb-4 pb-4 border-b border-slate-700">
                            <div className="flex items-center gap-3 flex-1">
                                <div className="p-2 bg-amber-900/30 rounded-lg text-amber-400">
                                    <Factory size={20} />
                                </div>
                                <Input
                                    className="w-full max-w-md"
                                    value={recipe.name}
                                    onChange={(e) =>
                                        updateRecipeName(
                                            recipe.id,
                                            e.target.value
                                        )
                                    }
                                />
                            </div>
                            <Button
                                variant="danger"
                                onClick={() =>
                                    setRecipes(
                                        recipes.filter(
                                            (r) => r.id !== recipe.id
                                        )
                                    )
                                }
                            >
                                <Trash2 size={16} />
                            </Button>
                        </div>

                        {/* IO Section */}
                        <div className="flex flex-col md:flex-row gap-4 items-start md:items-center mb-4">
                            {["inputs", "outputs"].map((type) => (
                                <React.Fragment key={type}>
                                    <div className="flex-1 w-full bg-slate-900/50 p-3 rounded border border-slate-700/50">
                                        <div className="flex justify-between items-center mb-2">
                                            <span className="text-xs font-bold text-slate-400 uppercase">
                                                {type}
                                            </span>
                                            <button
                                                onClick={() =>
                                                    modifyRecipeList(
                                                        recipe.id,
                                                        type,
                                                        "add"
                                                    )
                                                }
                                                className="text-blue-400 hover:text-blue-300 text-xs flex items-center gap-1"
                                            >
                                                <Plus size={12} /> Add
                                            </button>
                                        </div>
                                        {recipe[type].map((item, idx) => (
                                            <div
                                                key={idx}
                                                className="flex gap-2 items-center mb-2"
                                            >
                                                <Input
                                                    type="number"
                                                    value={item.amount}
                                                    onChange={(e) =>
                                                        modifyRecipeList(
                                                            recipe.id,
                                                            type,
                                                            "update",
                                                            idx,
                                                            "amount",
                                                            parseFloat(
                                                                e.target.value
                                                            )
                                                        )
                                                    }
                                                    className="w-20"
                                                />
                                                <Select
                                                    value={item.resourceId}
                                                    onChange={(e) =>
                                                        modifyRecipeList(
                                                            recipe.id,
                                                            type,
                                                            "update",
                                                            idx,
                                                            "resourceId",
                                                            e.target.value
                                                        )
                                                    }
                                                    options={resources.map(
                                                        (r) => ({
                                                            value: r.id,
                                                            label: r.name,
                                                        })
                                                    )}
                                                    className="flex-1"
                                                />
                                                <button
                                                    onClick={() =>
                                                        modifyRecipeList(
                                                            recipe.id,
                                                            type,
                                                            "remove",
                                                            idx
                                                        )
                                                    }
                                                    className="text-slate-500 hover:text-red-400"
                                                >
                                                    <Trash2 size={14} />
                                                </button>
                                            </div>
                                        ))}
                                    </div>
                                    {type === "inputs" && (
                                        <div className="text-slate-500 hidden md:block">
                                            <ArrowRight size={24} />
                                        </div>
                                    )}
                                </React.Fragment>
                            ))}
                        </div>

                        {/* Workforce Section */}
                        <div className="w-full bg-slate-900/30 p-3 rounded border border-slate-700/30">
                            <div className="flex justify-between items-center mb-2">
                                <div className="flex items-center gap-2">
                                    <Users
                                        size={16}
                                        className="text-slate-400"
                                    />
                                    <span className="text-xs font-bold text-slate-400 uppercase">
                                        Workforce
                                    </span>
                                </div>
                                <button
                                    onClick={() =>
                                        modifyRecipeWorkforce(recipe.id, "add")
                                    }
                                    className="text-purple-400 hover:text-purple-300 text-xs flex items-center gap-1"
                                >
                                    <Plus size={12} /> Add Pop
                                </button>
                            </div>
                            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-2">
                                {(recipe.workforce || []).map((item, idx) => (
                                    <div
                                        key={idx}
                                        className="flex gap-2 items-center bg-slate-900 border border-slate-700 rounded p-2"
                                    >
                                        <Input
                                            type="number"
                                            value={item.amount}
                                            onChange={(e) =>
                                                modifyRecipeWorkforce(
                                                    recipe.id,
                                                    "update",
                                                    idx,
                                                    "amount",
                                                    parseFloat(e.target.value)
                                                )
                                            }
                                            className="w-20 text-sm"
                                        />
                                        <Select
                                            value={item.popId}
                                            onChange={(e) =>
                                                modifyRecipeWorkforce(
                                                    recipe.id,
                                                    "update",
                                                    idx,
                                                    "popId",
                                                    e.target.value
                                                )
                                            }
                                            options={popTypes.map((p) => ({
                                                value: p.id,
                                                label: p.name,
                                            }))}
                                            className="w-full text-sm"
                                        />
                                        <button
                                            onClick={() =>
                                                modifyRecipeWorkforce(
                                                    recipe.id,
                                                    "remove",
                                                    idx
                                                )
                                            }
                                            className="text-slate-500 hover:text-red-400"
                                        >
                                            <Trash2 size={14} />
                                        </button>
                                    </div>
                                ))}
                            </div>
                        </div>
                    </Card>
                ))}
            </div>
        </div>
    );

    const renderGenericNeedsTab = (
        title,
        description,
        list,
        setList,
        prefix,
        icon,
        color
    ) => (
        <div className="space-y-4">
            <div className="flex justify-between items-center mb-6">
                <div>
                    <h2 className="text-2xl font-bold text-white">{title}</h2>
                    <p className="text-slate-400">{description}</p>
                </div>
                <Button
                    onClick={() =>
                        addEntity(
                            setList,
                            list,
                            prefix,
                            `New ${title.slice(0, -1)}`
                        )
                    }
                >
                    <Plus size={16} /> New Entry
                </Button>
            </div>
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
                {list.map((entity) => (
                    <NeedsEditor
                        key={entity.id}
                        entity={entity}
                        setFunction={setList}
                        list={list}
                        icon={icon}
                        colorTheme={color}
                    />
                ))}
            </div>
        </div>
    );

    const renderData = () => (
        <div className="max-w-4xl mx-auto space-y-6">
            <div className="text-center mb-8">
                <h2 className="text-2xl font-bold text-white mb-2">
                    Data Persistence
                </h2>
                <p className="text-slate-400">
                    Import or Export your Stella Invicta economic database.
                </p>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <Card className="p-6 flex flex-col items-center text-center gap-4">
                    <div className="p-4 bg-emerald-900/30 rounded-full text-emerald-400">
                        <Download size={32} />
                    </div>
                    <div className="w-full">
                        <Button
                            onClick={exportData}
                            className="w-full justify-center"
                        >
                            Download JSON
                        </Button>
                    </div>
                    <Button
                        onClick={copyToClipboard}
                        variant="secondary"
                        className="w-full justify-center"
                    >
                        Copy Text
                    </Button>
                </Card>
                <Card className="p-6 flex flex-col items-center text-center gap-4">
                    <div className="p-4 bg-blue-900/30 rounded-full text-blue-400">
                        <Upload size={32} />
                    </div>
                    <label className="w-full">
                        <div className="flex items-center justify-center w-full px-4 py-2 bg-slate-700 hover:bg-slate-600 text-slate-200 rounded-md cursor-pointer transition-colors gap-2 font-medium text-sm">
                            <Upload size={16} /> Select File
                        </div>
                        <input
                            type="file"
                            className="hidden"
                            accept=".json"
                            onChange={importData}
                        />
                    </label>
                </Card>
            </div>
            <div className="mt-8">
                <h3 className="text-sm font-bold text-slate-500 uppercase mb-2">
                    Raw Data Preview
                </h3>
                <div className="bg-slate-950 p-4 rounded-lg border border-slate-800 font-mono text-xs text-slate-300 overflow-auto max-h-96">
                    <pre>
                        {JSON.stringify(
                            {
                                resources,
                                recipes,
                                popTypes,
                                species,
                                cultures,
                                religions,
                            },
                            null,
                            2
                        )}
                    </pre>
                </div>
            </div>
        </div>
    );

    return (
        <div className="min-h-screen bg-slate-950 text-slate-200 font-sans selection:bg-blue-500/30">
            {/* Header */}
            <header className="bg-slate-900 border-b border-slate-800 sticky top-0 z-10">
                <div className="max-w-7xl mx-auto px-4 h-16 flex items-center justify-between">
                    <div className="flex items-center gap-3">
                        <div className="w-8 h-8 bg-gradient-to-br from-blue-500 to-purple-600 rounded-lg flex items-center justify-center shadow-lg shadow-blue-500/20">
                            <Settings className="text-white" size={18} />
                        </div>
                        <h1 className="font-bold text-xl tracking-tight text-white hidden md:block">
                            Stella Invicta{" "}
                            <span className="text-slate-500 font-normal">
                                | Economic Architect
                            </span>
                        </h1>
                        <h1 className="font-bold text-xl tracking-tight text-white md:hidden">
                            SI | Architect
                        </h1>
                    </div>
                </div>

                {/* Navigation Bar */}
                <div className="max-w-7xl mx-auto px-4 border-t border-slate-800 bg-slate-900 overflow-x-auto">
                    <div className="flex items-center gap-1 py-2">
                        <button
                            onClick={() => setActiveTab("resources")}
                            className={`px-3 py-2 rounded-md text-sm font-medium flex items-center gap-2 whitespace-nowrap transition-all ${
                                activeTab === "resources"
                                    ? "bg-slate-700 text-white"
                                    : "text-slate-400 hover:bg-slate-800"
                            }`}
                        >
                            <Package size={16} /> Resources
                        </button>
                        <button
                            onClick={() => setActiveTab("production")}
                            className={`px-3 py-2 rounded-md text-sm font-medium flex items-center gap-2 whitespace-nowrap transition-all ${
                                activeTab === "production"
                                    ? "bg-slate-700 text-white"
                                    : "text-slate-400 hover:bg-slate-800"
                            }`}
                        >
                            <Factory size={16} /> Production
                        </button>
                        <div className="w-px h-6 bg-slate-800 mx-2" />
                        <button
                            onClick={() => setActiveTab("pops")}
                            className={`px-3 py-2 rounded-md text-sm font-medium flex items-center gap-2 whitespace-nowrap transition-all ${
                                activeTab === "pops"
                                    ? "bg-purple-900/50 text-purple-200"
                                    : "text-slate-400 hover:bg-slate-800"
                            }`}
                        >
                            <Users size={16} /> Classes
                        </button>
                        <button
                            onClick={() => setActiveTab("species")}
                            className={`px-3 py-2 rounded-md text-sm font-medium flex items-center gap-2 whitespace-nowrap transition-all ${
                                activeTab === "species"
                                    ? "bg-emerald-900/50 text-emerald-200"
                                    : "text-slate-400 hover:bg-slate-800"
                            }`}
                        >
                            <Dna size={16} /> Species
                        </button>
                        <button
                            onClick={() => setActiveTab("cultures")}
                            className={`px-3 py-2 rounded-md text-sm font-medium flex items-center gap-2 whitespace-nowrap transition-all ${
                                activeTab === "cultures"
                                    ? "bg-orange-900/50 text-orange-200"
                                    : "text-slate-400 hover:bg-slate-800"
                            }`}
                        >
                            <Globe size={16} /> Cultures
                        </button>
                        <button
                            onClick={() => setActiveTab("religions")}
                            className={`px-3 py-2 rounded-md text-sm font-medium flex items-center gap-2 whitespace-nowrap transition-all ${
                                activeTab === "religions"
                                    ? "bg-rose-900/50 text-rose-200"
                                    : "text-slate-400 hover:bg-slate-800"
                            }`}
                        >
                            <BookOpen size={16} /> Religions
                        </button>
                        <div className="w-px h-6 bg-slate-800 mx-2" />
                        <button
                            onClick={() => setActiveTab("data")}
                            className={`px-3 py-2 rounded-md text-sm font-medium flex items-center gap-2 whitespace-nowrap transition-all ${
                                activeTab === "data"
                                    ? "bg-slate-700 text-white"
                                    : "text-slate-400 hover:bg-slate-800"
                            }`}
                        >
                            <Database size={16} /> Data
                        </button>
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
                    {activeTab === "resources" && renderResources()}
                    {activeTab === "production" && renderProduction()}
                    {activeTab === "pops" &&
                        renderGenericNeedsTab(
                            "Social Classes",
                            "Define jobs and social strata.",
                            popTypes,
                            setPopTypes,
                            "pop",
                            Users,
                            "purple"
                        )}
                    {activeTab === "species" &&
                        renderGenericNeedsTab(
                            "Species",
                            "Define biological needs.",
                            species,
                            setSpecies,
                            "specie",
                            Dna,
                            "emerald"
                        )}
                    {activeTab === "cultures" &&
                        renderGenericNeedsTab(
                            "Cultures",
                            "Define cultural preferences.",
                            cultures,
                            setCultures,
                            "cul",
                            Globe,
                            "orange"
                        )}
                    {activeTab === "religions" &&
                        renderGenericNeedsTab(
                            "Religions",
                            "Define spiritual requirements.",
                            religions,
                            setReligions,
                            "rel",
                            BookOpen,
                            "rose"
                        )}
                    {activeTab === "data" && renderData()}
                </div>
            </main>
        </div>
    );
}
