import React from "react";
import { Settings, X, Trash2, MousePointer2 } from "lucide-react";
import { STAR_TYPES } from "../utils/constants";

const SystemInspector = ({
	selectedSystem,
	selectedCount,
	empires,
	onUpdateSystem,
	onDeleteSelected,
	onClearSelection,
	onBulkUpdateEmpire,
}) => {
	if (selectedSystem) {
		return (
			<div className="absolute right-4 top-4 w-72 bg-slate-900/95 backdrop-blur-xl border border-slate-700 rounded-2xl shadow-2xl overflow-hidden flex flex-col animate-in slide-in-from-right-10 duration-200">
				{/* Header */}
				<div className="p-4 border-b border-slate-700 flex items-center justify-between bg-slate-800/50">
					<div className="flex items-center gap-2">
						<Settings size={18} className="text-indigo-400" />
						<h2 className="font-semibold text-white">System Details</h2>
					</div>
					<button
						onClick={onClearSelection}
						className="text-slate-400 hover:text-white transition-colors"
					>
						<X size={18} />
					</button>
				</div>

				{/* Content */}
				<div className="p-5 flex flex-col gap-6">
					{/* Name Input */}
					<div className="flex flex-col gap-2">
						<label className="text-xs font-medium text-slate-400 uppercase tracking-wider">
							System Name
						</label>
						<input
							autoFocus
							type="text"
							value={selectedSystem.name}
							onChange={(e) =>
								onUpdateSystem(selectedSystem.id, {
									name: e.target.value,
								})
							}
							className="bg-slate-950 border border-slate-700 rounded-lg px-3 py-2 text-white focus:ring-2 focus:ring-indigo-500 focus:border-transparent outline-none transition-all placeholder-slate-600"
						/>
					</div>

					{/* Empire Selector in Inspector */}
					<div className="flex flex-col gap-2">
						<label className="text-xs font-medium text-slate-400 uppercase tracking-wider">
							Occupying Empire
						</label>
						<select
							value={selectedSystem.ownerId || ""}
							onChange={(e) =>
								onUpdateSystem(selectedSystem.id, {
									ownerId: e.target.value || null,
								})
							}
							className="bg-slate-950 border border-slate-700 rounded-lg px-3 py-2 text-white focus:ring-2 focus:ring-indigo-500 outline-none"
						>
							<option value="">Unclaimed Space</option>
							{empires.map((e) => (
								<option key={e.id} value={e.id}>
									{e.name}
								</option>
							))}
						</select>
					</div>

					{/* System Influence Slider */}
					{selectedSystem.ownerId && (
						<div className="flex flex-col gap-2">
							<div className="flex justify-between items-center">
								<label className="text-xs font-medium text-slate-400 uppercase tracking-wider">
									Local Influence
								</label>
								<span className="text-xs font-mono text-indigo-400">
									+{selectedSystem.influence || 0}
								</span>
							</div>
							<input
								type="range"
								min="0"
								max="100"
								step="5"
								value={selectedSystem.influence || 0}
								onChange={(e) =>
									onUpdateSystem(selectedSystem.id, {
										influence: parseInt(e.target.value),
									})
								}
								className="w-full h-2 bg-slate-800 rounded-lg appearance-none cursor-pointer accent-indigo-500"
							/>
						</div>
					)}

					{/* Star Type Selector */}
					<div className="flex flex-col gap-2">
						<label className="text-xs font-medium text-slate-400 uppercase tracking-wider">
							Star Class
						</label>
						<div className="grid grid-cols-5 gap-2">
							{STAR_TYPES.map((type) => (
								<button
									key={type.id}
									onClick={() =>
										onUpdateSystem(selectedSystem.id, { type: type.id })
									}
									className={`aspect-square rounded-full border-2 flex items-center justify-center transition-all ${
										selectedSystem.type === type.id
											? "border-indigo-400 scale-110 shadow-[0_0_10px_rgba(99,102,241,0.5)]"
											: "border-transparent hover:scale-105 hover:border-slate-600"
									}`}
									style={{ backgroundColor: "#1e293b" }}
									title={type.label}
								>
									<div
										className="w-4 h-4 rounded-full"
										style={{
											backgroundColor: type.color,
											boxShadow: `0 0 8px ${type.glow}`,
										}}
									/>
								</button>
							))}
						</div>
						<div className="text-xs text-center text-slate-500 mt-1">
							{STAR_TYPES.find((t) => t.id === selectedSystem.type)?.label}
						</div>
					</div>

					{/* Coordinates */}
					<div className="grid grid-cols-2 gap-4">
						<div className="bg-slate-950/50 p-2 rounded border border-slate-800">
							<div className="text-[10px] text-slate-500">X COORD</div>
							<div className="font-mono text-sm">
								{Math.round(selectedSystem.x)}
							</div>
						</div>
						<div className="bg-slate-950/50 p-2 rounded border border-slate-800">
							<div className="text-[10px] text-slate-500">Y COORD</div>
							<div className="font-mono text-sm">
								{Math.round(selectedSystem.y)}
							</div>
						</div>
					</div>

					{/* Actions */}
					<div className="pt-4 border-t border-slate-800">
						<button
							onClick={onDeleteSelected}
							className="w-full flex items-center justify-center gap-2 bg-red-500/10 hover:bg-red-500/20 text-red-400 hover:text-red-300 py-2 rounded-lg transition-colors border border-red-500/20"
						>
							<Trash2 size={16} />
							<span>Delete System</span>
						</button>
					</div>
				</div>
			</div>
		);
	}

	if (selectedCount > 1) {
		return (
			<div className="absolute right-4 top-4 w-72 bg-slate-900/95 backdrop-blur-xl border border-slate-700 rounded-2xl shadow-2xl p-4 animate-in slide-in-from-right-10">
				<div className="flex items-center justify-between mb-4">
					<h2 className="font-semibold text-white">
						{selectedCount} Systems Selected
					</h2>
					<button onClick={onClearSelection}>
						<X size={18} className="text-slate-400 hover:text-white" />
					</button>
				</div>

				{/* Bulk Empire Assignment */}
				<div className="mb-4 space-y-2">
					<label className="text-xs font-medium text-slate-400 uppercase tracking-wider">
						Set Empire
					</label>
					<select
						onChange={(e) => onBulkUpdateEmpire(e.target.value || null)}
						className="w-full bg-slate-950 border border-slate-700 rounded-lg px-3 py-2 text-white focus:ring-2 focus:ring-indigo-500 outline-none"
					>
						<option value="">-- No Change --</option>
						<option value="">Unclaimed Space</option>
						{empires.map((e) => (
							<option key={e.id} value={e.id}>
								{e.name}
							</option>
						))}
					</select>
				</div>

				<button
					onClick={onDeleteSelected}
					className="w-full flex items-center justify-center gap-2 bg-red-500/10 hover:bg-red-500/20 text-red-400 hover:text-red-300 py-2 rounded-lg transition-colors border border-red-500/20"
				>
					<Trash2 size={16} />
					<span>Delete All Selected</span>
				</button>
			</div>
		);
	}

	return (
		<div className="hidden md:block absolute right-4 top-4 pointer-events-none opacity-50">
			<div className="bg-slate-900/50 backdrop-blur p-4 rounded-xl border border-slate-800 text-slate-500 text-sm flex items-center gap-2">
				<MousePointer2 size={16} /> Select a system to edit
			</div>
		</div>
	);
};

export default SystemInspector;
