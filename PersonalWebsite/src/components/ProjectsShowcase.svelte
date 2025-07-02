<!-- src/components/ProjectsShowcase.svelte -->
<script>
    import { onMount } from 'svelte';

    let projects = [];
    let isLoading = true;

    // --- Data Fetching ---
    onMount(async () => {
        try {
            const response = await fetch('/projects.json');
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            projects = await response.json();
        } catch (error) {
            console.error("Failed to fetch projects:", error);
            // Optionally, set an error state to show a message in the UI
        } finally {
            isLoading = false;
        }
    });

    // --- Helper for styling status badges ---
    const statusColors = {
        "Completed": "bg-green-100 text-green-800",
        "In Progress": "bg-blue-100 text-blue-800",
        "On Hold": "bg-yellow-100 text-yellow-800",
    };
</script>

{#if isLoading}
    <p class="text-center text-gray-500">Loading projects...</p>
{:else}
    <!-- CHANGE: The grid is now always single-column, with a max-width and centered. -->
    <div class="grid grid-cols-1 gap-8 max-w-4xl mx-auto">
        {#each projects as project (project.title)}
            <div class="bg-white rounded-lg shadow-lg overflow-hidden flex flex-col transition-transform hover:scale-[1.02] hover:shadow-xl">
                <div class="p-6 flex-grow">
                    <div class="flex justify-between items-start mb-4">
                        <h3 class="text-xl font-bold text-gray-900">{project.title}</h3>
                        {#if project.status}
                            <span class="text-xs font-semibold px-2.5 py-0.5 rounded-full {statusColors[project.status] || 'bg-gray-100 text-gray-800'}">
                                {project.status}
                            </span>
                        {/if}
                    </div>
                    <p class="text-gray-700 mb-4 text-sm leading-relaxed">{project.description}</p>
                </div>
                
                <div class="px-6 pt-4 pb-6 bg-gray-50">
                    <!-- Technology Tags -->
                    {#if project.technologies && project.technologies.length}
                        <div class="mb-4">
                            <h4 class="text-xs font-semibold text-gray-500 uppercase mb-2">Technologies</h4>
                            <div class="flex flex-wrap gap-2">
                                {#each project.technologies as tech}
                                    <span class="bg-gray-200 text-gray-800 text-xs font-medium px-2 py-1 rounded-md">
                                        {tech}
                                    </span>
                                {/each}
                            </div>
                        </div>
                    {/if}
                    
                    <!-- Project Links -->
                    {#if project.links && project.links.length}
                        <div class="flex items-center gap-4">
                             {#each project.links as link}
                                <a href={link.url} target="_blank" rel="noopener noreferrer" class="inline-flex items-center gap-2 text-sm font-semibold text-blue-600 hover:text-blue-800 transition-colors">
                                    {#if link.name === 'GitHub'}
                                        <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16"><path d="M8 0C3.58 0 0 3.58 0 8c0 3.54 2.29 6.53 5.47 7.59.4.07.55-.17.55-.38 0-.19-.01-.82-.01-1.49-2.01.37-2.53-.49-2.69-.94-.09-.23-.48-.94-.82-1.13-.28-.15-.68-.52-.01-.53.63-.01 1.08.58 1.23.82.72 1.21 1.87.87 2.33.66.07-.52.28-.87.51-1.07-1.78-.2-3.64-.89-3.64-3.95 0-.87.31-1.59.82-2.15-.08-.2-.36-1.02.08-2.12 0 0 .67-.21 2.2.82.64-.18 1.32-.27 2-.27.68 0 1.36.09 2 .27 1.53-1.04 2.2-.82 2.2-.82.44 1.1.16 1.92.08 2.12.51.56.82 1.27.82 2.15 0 3.07-1.87 3.75-3.65 3.95.29.25.54.73.54 1.48 0 1.07-.01 1.93-.01 2.2 0 .21.15.46.55.38A8.012 8.012 0 0 0 16 8c0-4.42-3.58-8-8-8z"/></svg>
                                    {:else if link.name === 'Live Demo'}
                                        <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16"><path d="M10.478 1.647a.5.5 0 1 0-.956-.294l-4 13a.5.5 0 0 0 .956.294l4-13zM4.854 4.146a.5.5 0 0 1 0 .708L1.707 8l3.147 3.146a.5.5 0 0 1-.708.708l-3.5-3.5a.5.5 0 0 1 0-.708l3.5-3.5a.5.5 0 0 1 .708 0zm6.292 0a.5.5 0 0 0 0 .708L14.293 8l-3.147 3.146a.5.5 0 0 0 .708.708l3.5-3.5a.5.5 0 0 0 0-.708l-3.5-3.5a.5.5 0 0 0-.708 0z"/></svg>
                                    {:else if link.name === 'NuGet'}
                                    <svg width="16" height="16" viewBox="0 0 512 512" fill="currentColor" version="1.1" xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink">
                                        <!-- Generator: Sketch 47.1 (45422) - http://www.bohemiancoding.com/sketch -->
                                        <title>nuget</title>
                                        <desc>Created with Sketch.</desc>
                                        <defs>
                                            <polygon id="path-1" points="0 46.021103 0 3.7002935 84.6521577 3.7002935 84.6521577 88.3419125 0 88.3419125"></polygon>
                                        </defs>
                                        <g id="Symbols" stroke="none" stroke-width="1" fill="none" fill-rule="evenodd">
                                            <g id="nuget">
                                                <g id="Global/Logo" transform="translate(0.000000, 6.000000)">
                                                    <path d="M374.424959,454.856991 C327.675805,454.856991 289.772801,416.950177 289.772801,370.196324 C289.772801,323.463635 327.675805,285.535656 374.424959,285.535656 C421.174113,285.535656 459.077116,323.463635 459.077116,370.196324 C459.077116,416.950177 421.174113,454.856991 374.424959,454.856991 M205.565067,260.814741 C176.33891,260.814741 152.657469,237.109754 152.657469,207.901824 C152.657469,178.672728 176.33891,154.988907 205.565067,154.988907 C234.791225,154.988907 258.472666,178.672728 258.472666,207.901824 C258.472666,237.109754 234.791225,260.814741 205.565067,260.814741 M378.170817,95.6417786 L236.886365,95.6417786 C164.889705,95.6417786 106.479717,154.057639 106.479717,226.082702 L106.479717,367.360191 C106.479717,439.40642 164.889705,497.77995 236.886365,497.77995 L378.170817,497.77995 C450.209803,497.77995 508.577466,439.40642 508.577466,367.360191 L508.577466,226.082702 C508.577466,154.057639 450.209803,95.6417786 378.170817,95.6417786" id="Fill-12" fill="#004880" fill-rule="evenodd"></path>
                                                    <mask id="mask-2" fill="white">
                                                        <use xlink:href="#path-1"></use>
                                                    </mask>
                                                    <g id="Clip-15"></g>
                                                    <path d="M84.6521577,46.0115787 C84.6521577,69.3990881 65.6900744,88.3419125 42.3260788,88.3419125 C18.9409203,88.3419125 0,69.3990881 0,46.0115787 C0,22.6452344 18.9409203,3.68124485 42.3260788,3.68124485 C65.6900744,3.68124485 84.6521577,22.6452344 84.6521577,46.0115787" id="Fill-14" fill="#004880" fill-rule="evenodd" mask="url(#mask-2)"></path>
                                                </g>
                                            </g>
                                        </g>
                                    </svg>
                                    {/if}
                                    {link.name}
                                </a>
                             {/each}
                        </div>
                    {/if}
                </div>
            </div>
        {/each}
    </div>
{/if}
