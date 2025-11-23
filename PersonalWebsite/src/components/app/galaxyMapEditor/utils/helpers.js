import {
	PREFIXES,
	SUFFIXES,
	TITLES,
	GOVERNMENT_TYPES,
	EMPIRE_ADJECTIVES,
	EMPIRE_NOUNS,
} from "./constants";

export const generateId = () => Math.random().toString(36).substr(2, 9);

export const getDistance = (p1, p2) => {
	return Math.sqrt(Math.pow(p2.x - p1.x, 2) + Math.pow(p2.y - p1.y, 2));
};

export const generateName = () => {
	const pre = PREFIXES[Math.floor(Math.random() * PREFIXES.length)];
	const suf = SUFFIXES[Math.floor(Math.random() * SUFFIXES.length)];
	const title =
		Math.random() > 0.8
			? TITLES[Math.floor(Math.random() * TITLES.length)]
			: "";
	return `${pre}${suf}${title}`;
};

export const generateEmpireName = () => {
	const rand = Math.random();
	const baseName = generateName().replace(
		/ (Prime|IV|VII|Alpha|Beta|Outpost)/,
		""
	); // Strip titles for empire names

	// Pattern 1: [Adjective] [BaseName] [Government] (e.g. United Sol Federation)
	if (rand < 0.25) {
		const adj =
			EMPIRE_ADJECTIVES[Math.floor(Math.random() * EMPIRE_ADJECTIVES.length)];
		const gov =
			GOVERNMENT_TYPES[Math.floor(Math.random() * GOVERNMENT_TYPES.length)];
		return `${adj} ${baseName} ${gov}`;
	}

	// Pattern 2: [Government] of [BaseName] (e.g. Republic of Vega)
	if (rand < 0.5) {
		const gov =
			GOVERNMENT_TYPES[Math.floor(Math.random() * GOVERNMENT_TYPES.length)];
		return `${gov} of ${baseName}`;
	}

	// Pattern 3: [BaseName] [Government] (e.g. Korlax Empire)
	if (rand < 0.75) {
		const gov =
			GOVERNMENT_TYPES[Math.floor(Math.random() * GOVERNMENT_TYPES.length)];
		return `${baseName} ${gov}`;
	}

	// Pattern 4: [BaseName] [Noun] (e.g. Cygni Systems)
	const noun = EMPIRE_NOUNS[Math.floor(Math.random() * EMPIRE_NOUNS.length)];
	return `${baseName} ${noun}`;
};
