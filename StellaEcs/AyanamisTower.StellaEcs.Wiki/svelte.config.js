import { vitePreprocess } from '@astrojs/svelte';
import mdx from "@astrojs/mdx";
import svelte from "@astrojs/svelte";

export default {
    preprocess: vitePreprocess(),
    integrations: [mdx(), svelte()],
};
