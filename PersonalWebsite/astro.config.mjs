// @ts-check
import { defineConfig } from "astro/config";

import svelte from "@astrojs/svelte";
import react from "@astrojs/react";

import tailwindcss from "@tailwindcss/vite";

import mdx from "@astrojs/mdx";
import sitemap from "@astrojs/sitemap";
// https://astro.build/config
export default defineConfig({
    integrations: [svelte(), react(), mdx(), sitemap()],

    vite: {
        plugins: [tailwindcss()],
    },
});
