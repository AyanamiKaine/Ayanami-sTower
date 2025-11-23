export function colorToHex(ctx, color) {
	// Helper to normalize HSL or other formats to Hex for input[type=color]
	// We need a context to do this trick, or we can use a temporary one
	if (!ctx) {
		const canvas = document.createElement("canvas");
		ctx = canvas.getContext("2d");
	}
	ctx.fillStyle = color;
	return ctx.fillStyle;
}

export function getRandomColor() {
	const hue = Math.floor(Math.random() * 360);
	return `hsl(${hue}, 60%, 50%)`;
}
