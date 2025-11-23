import React from "react";
import { Sparkles, X, Dice5 } from "lucide-react";

const GalaxyGenerator = ({ config, onConfigChange, onGenerate, onClose }) => {
	return (
		<div className="absolute top-4 left-20 z-30 w-80 bg-slate-900/95 backdrop-blur-xl border border-slate-700 rounded-2xl shadow-2xl overflow-hidden flex flex-col animate-in slide-in-from-left-10 duration-200">
			<div className="p-4 border-b border-slate-700 flex items-center justify-between bg-slate-800/50">
				<div className="flex items-center gap-2">
					<Sparkles size={18} className="text-yellow-400" />
					<h2 className="font-semibold text-white">Generate Galaxy</h2>
				</div>
				<button onClick={onClose}>
					<X size={18} className="text-slate-400 hover:text-white" />
				</button>
			</div>

			<div className="p-5 flex flex-col gap-5">
				<div className="space-y-2">
					<label className="text-xs font-medium text-slate-400 uppercase">
						Shape
					</label>
					<div className="grid grid-cols-3 gap-2">
						{["spiral", "ring", "irregular"].map((t) => (
							<button
								key={t}
								onClick={() => onConfigChange({ ...config, type: t })}
								className={`px-2 py-1.5 text-xs rounded border capitalize ${
									config.type === t
										? "bg-indigo-600 border-indigo-500 text-white"
										: "bg-slate-800 border-slate-700 text-slate-400 hover:bg-slate-700"
								}`}
							>
								{t}
							</button>
						))}
					</div>
				</div>

				<div className="space-y-2">
					<div className="flex justify-between text-xs text-slate-400">
						<label className="uppercase font-medium">System Count</label>
						<span>{config.count}</span>
					</div>
					<input
						type="range"
						min="10"
						max="500"
						step="10"
						value={config.count}
						onChange={(e) =>
							onConfigChange({
								...config,
								count: parseInt(e.target.value),
							})
						}
						className="w-full accent-indigo-500 h-1.5 bg-slate-700 rounded-lg appearance-none cursor-pointer"
					/>
				</div>

				<div className="space-y-2">
					<div className="flex justify-between text-xs text-slate-400">
						<label className="uppercase font-medium">Galaxy Radius</label>
						<span>{config.radius}</span>
					</div>
					<input
						type="range"
						min="200"
						max="2000"
						step="100"
						value={config.radius}
						onChange={(e) =>
							onConfigChange({
								...config,
								radius: parseInt(e.target.value),
							})
						}
						className="w-full accent-indigo-500 h-1.5 bg-slate-700 rounded-lg appearance-none cursor-pointer"
					/>
				</div>

				<div className="pt-2">
					<button
						onClick={onGenerate}
						className="w-full py-2.5 bg-gradient-to-r from-indigo-600 to-purple-600 hover:from-indigo-500 hover:to-purple-500 text-white rounded-lg font-medium shadow-lg shadow-indigo-500/20 transition-all active:scale-95 flex items-center justify-center gap-2"
					>
						<Dice5 size={18} /> Generate
					</button>
					<p className="text-[10px] text-center text-slate-500 mt-2">
						Warning: This will replace your current map.
					</p>
				</div>
			</div>
		</div>
	);
};

export default GalaxyGenerator;
