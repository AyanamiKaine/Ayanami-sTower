// @ts-check
import { defineConfig } from "astro/config";

import svelte from "@astrojs/svelte";

import tailwindcss from "@tailwindcss/vite";

import mdx from "@astrojs/mdx";
import sitemap from "@astrojs/sitemap";
// https://astro.build/config
export default defineConfig({
    integrations: [svelte(), mdx(), sitemap()
    ],

    vite: {
        plugins: [tailwindcss()],
    },
});