import React from "react";
import { Shield, X, Trash2, Signal, Paintbrush, Plus } from "lucide-react";

const EmpireManager = ({
	empires,
	activeEmpireId,
	tool,
	onClose,
	onCreateEmpire,
	onDeleteEmpire,
	onUpdateEmpire,
	onSetActiveEmpire,
	onSetTool,
}) => {
	return (
		<div className="absolute top-4 left-20 z-30 w-80 bg-slate-900/95 backdrop-blur-xl border border-slate-700 rounded-2xl shadow-2xl overflow-hidden flex flex-col animate-in slide-in-from-left-10 duration-200 max-h-[80vh]">
			<div className="p-4 border-b border-slate-700 flex items-center justify-between bg-slate-800/50">
				<div className="flex items-center gap-2">
					<Shield size={18} className="text-indigo-400" />
					<h2 className="font-semibold text-white">Empires</h2>
				</div>
				<button onClick={onClose}>
					<X size={18} className="text-slate-400 hover:text-white" />
				</button>
			</div>

			<div className="p-4 flex flex-col gap-3 overflow-y-auto">
				{empires.length === 0 && (
					<div className="text-center py-4 text-slate-500 text-sm italic">
						No empires defined. Create one to start painting borders.
					</div>
				)}

				{empires.map((empire) => (
					<div
						key={empire.id}
						className={`p-3 rounded-xl border transition-all ${
							activeEmpireId === empire.id
								? "bg-slate-800 border-indigo-500 shadow-lg shadow-indigo-900/20"
								: "bg-slate-950/50 border-slate-800 hover:border-slate-700"
						}`}
					>
						<div className="flex items-center gap-2 mb-2">
							<input
								type="color"
								value={empire.color}
								onChange={(e) =>
									onUpdateEmpire(empire.id, { color: e.target.value })
								}
								className="w-6 h-6 rounded overflow-hidden border-none bg-transparent cursor-pointer"
							/>
							<input
								type="text"
								value={empire.name}
								onChange={(e) =>
									onUpdateEmpire(empire.id, { name: e.target.value })
								}
								className="bg-transparent border-b border-transparent hover:border-slate-700 focus:border-indigo-500 outline-none text-sm font-medium w-full"
							/>
							<button
								onClick={() => onDeleteEmpire(empire.id)}
								className="text-slate-600 hover:text-red-400"
							>
								<Trash2 size={14} />
							</button>
						</div>

						{/* Influence Slider */}
						<div className="flex items-center gap-2 mb-3 px-1">
							<Signal size={12} className="text-slate-500" />
							<div className="flex-1 flex flex-col">
								<div className="flex justify-between text-[10px] text-slate-400 uppercase font-medium">
									<span>Influence</span>
									<span>{empire.influence || 50}</span>
								</div>
								<input
									type="range"
									min="20"
									max="150"
									step="5"
									value={empire.influence || 50}
									onChange={(e) =>
										onUpdateEmpire(empire.id, {
											influence: parseInt(e.target.value),
										})
									}
									className="w-full accent-indigo-500 h-1 bg-slate-700 rounded-lg appearance-none cursor-pointer mt-1"
								/>
							</div>
						</div>

						<div className="flex items-center gap-2">
							<button
								onClick={() => {
									onSetActiveEmpire(empire.id);
									onSetTool("paint");
								}}
								className={`flex-1 flex items-center justify-center gap-2 text-xs py-1.5 rounded transition-colors ${
									activeEmpireId === empire.id && tool === "paint"
										? "bg-indigo-600 text-white"
										: "bg-slate-800 text-slate-400 hover:bg-slate-700"
								}`}
							>
								<Paintbrush size={12} />{" "}
								{activeEmpireId === empire.id && tool === "paint"
									? "Painting..."
									: "Paint Territory"}
							</button>
						</div>
					</div>
				))}

				<button
					onClick={onCreateEmpire}
					className="mt-2 flex items-center justify-center gap-2 py-2 border border-dashed border-slate-700 rounded-xl text-slate-400 hover:text-white hover:border-slate-500 hover:bg-slate-800/50 transition-all text-sm"
				>
					<Plus size={14} /> Create New Empire
				</button>
			</div>
		</div>
	);
};

export default EmpireManager;
