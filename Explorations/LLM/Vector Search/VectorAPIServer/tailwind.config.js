/** @type {import('tailwindcss').Config} */
export default {
    content: ["./src/**/*.{astro,html,js,jsx,ts,tsx,svelte}"],
    theme: {
        extend: {
            fontFamily: {
                sans: ["Inter", "system-ui", "sans-serif"],
            },
            colors: {
                brand: {
                    50: "#f2f8ff",
                    100: "#e6f0fe",
                    200: "#c3defd",
                    300: "#9fcafa",
                    400: "#58a3f6",
                    500: "#1d7ff0",
                    600: "#0c62ce",
                    700: "#0c4da2",
                    800: "#0f3f7f",
                    900: "#112f57",
                },
            },
            boxShadow: {
                soft: "0 1px 2px rgba(0,0,0,0.06), 0 2px 4px rgba(0,0,0,0.04)",
            },
        },
    },
    plugins: [],
};
