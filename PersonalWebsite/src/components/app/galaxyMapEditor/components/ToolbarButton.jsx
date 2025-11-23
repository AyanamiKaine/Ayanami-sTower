import React from "react";

const ToolbarButton = ({ active, onClick, icon: Icon, label, shortcut }) => (
	<div className="group relative flex items-center">
		<button
			onClick={onClick}
			className={`p-3 rounded-xl transition-all duration-200 shadow-sm ${
				active
					? "bg-indigo-600 text-white shadow-indigo-500/30"
					: "bg-slate-800 text-slate-400 hover:bg-slate-700 hover:text-white hover:shadow-lg"
			}`}
			title={`${label} ${shortcut ? `(${shortcut})` : ""}`}
		>
			<Icon size={20} strokeWidth={active ? 2.5 : 2} />
		</button>
		<div className="absolute left-full ml-3 px-2 py-1 bg-slate-900 text-xs text-white rounded opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap pointer-events-none z-50 border border-slate-700">
			{label} <span className="text-slate-500 ml-1">{shortcut}</span>
		</div>
	</div>
);

export default ToolbarButton;
