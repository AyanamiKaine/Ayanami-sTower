// @ts-check
import { defineConfig } from "astro/config";

import svelte from "@astrojs/svelte";

import tailwindcss from "@tailwindcss/vite";

import mdx from "@astrojs/mdx";
import sitemap from "@astrojs/sitemap";
import node from "@astrojs/node";

// https://astro.build/config
export default defineConfig({
    output: "server", // Enable server-side rendering
    adapter: node({
        mode: "standalone",
    }),
    integrations: [svelte(), mdx(), sitemap()],

    vite: {
        plugins: [tailwindcss()],
    },
});
