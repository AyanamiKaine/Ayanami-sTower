export const GET= async ({ }) => {
    // Retrieve all pages from the /pages/ directory.
    // We are looking for .md, .mdx, and .astro files.
    const allPages = await import.meta.glob(['../**/*.md', '../**/*.mdx', '../**/*.astro']);

    const posts = [];

    for (const path in allPages) {
        const mod = await allPages[path]();
        let title = '';
        let description = '';
        let summary = '';
        let tags = [];
        let url = '';

        // Skip layouts, API routes, and other non-page files
        if (path.includes('/layouts/') || path.includes('/api/') || path.includes('/components/')) {
            continue;
        }

        // Handle Markdown and MDX files
        if (path.endsWith('.md') || path.endsWith('.mdx')) {
            if (mod.frontmatter && mod.frontmatter.title && !mod.frontmatter.draft) {
                title = mod.frontmatter.title;
                description = mod.frontmatter.description || '';
                summary = mod.frontmatter.summary || '';
                tags = mod.frontmatter.tags || [];
            }
        }
        // Handle Astro files
        else if (path.endsWith('.astro')) {
            // For .astro files, we look for an exported title.
            // This is a convention you'll need to adopt for your .astro pages.
            if (mod.title && !mod.draft) {
                title = mod.title;
                description = mod.description || '';
                summary = mod.summary || '';
                tags = mod.tags || [];
            }
        }

        if (title) {
            // Generate the URL from the file path
            url = path
                .replace('../', '/') // remove ../
                .replace(/\.(md|mdx|astro)$/, '') // remove file extension
                .replace(/\/index$/, ''); // remove /index from the end

            // Ensure the root index page is just "/"
            if (url === '') {
                url = '/';
            }

            posts.push({ url, title, description, summary, tags });
        }
    }

    // Sort posts alphabetically by title
    const sortedPosts = posts.sort((a, b) => a.title.localeCompare(b.title));

    return new Response(JSON.stringify(sortedPosts), {
        status: 200,
        headers: {
            'Content-Type': 'application/json',
        },
    });
};
