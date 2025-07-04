<!-- src/components/Search.svelte -->
<script lang="ts">
    import { onMount } from "svelte";
    import Fuse from "fuse.js";

    // State variables
    let allPages: any[] = [];
    let fuse: Fuse<any> | null = null;
    let searchQuery = "";
    let results: any[] = [];
    let isLoading = true;
    let isFocused = false;

    // Fetch the search index when the component mounts
    onMount(async () => {
        try {
            const response = await fetch("/api/search.json");
            if (!response.ok) {
                throw new Error(
                    `Failed to fetch search data. Status: ${response.status}`,
                );
            }
            allPages = await response.json();

            if (allPages.length === 0) {
                console.warn(
                    "Warning: Search index is empty. Check your /api/search.json.ts endpoint and markdown files.",
                ); // DEBUG WARNING
            }

            // Configure Fuse.js
            const options = {
                keys: ["title", "description", "summary", "tags"],
                includeScore: true,
                threshold: 0.4,
                minMatchCharLength: 2,
            };
            fuse = new Fuse(allPages, options);
        } catch (error) {
            console.error("Error initializing search component:", error); // Improved error logging
        } finally {
            isLoading = false;
        }
    });

    // Reactive statement: This code runs whenever `searchQuery` changes.
    $: {
        if (fuse && searchQuery.length > 1) {
            const searchResults = fuse.search(searchQuery).slice(0, 10);
            results = searchResults.map((result) => result.item);
        } else {
            // Clear results if the query is too short
            results = [];
        }
    }

    function handleFocus() {
        isFocused = true;
    }

    function handleBlur() {
        // We use a short timeout to allow clicking on a result link before the results disappear.
        setTimeout(() => {
            isFocused = false;
        }, 150);
    }

    function clearSearch() {
        searchQuery = "";
        results = [];
    }

    // Helper function to truncate text
    function truncate(text: string, length: number) {
        if (text?.length > length) {
            return text.slice(0, length) + "...";
        }
        return text;
    }

    function navigateTo(url: string) {
        window.location.href = url;
    }
</script>

<div class="relative w-full max-w-md mx-auto">
    <!-- Search Input -->
    <div class="relative">
        <input
            type="text"
            placeholder="Search"
            class="w-full px-4 bg-white border-2 border-gray-300 rounded-lg shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-colors"
            bind:value={searchQuery}
            on:focus={handleFocus}
            on:blur={handleBlur}
            aria-label="Search"
        />
        <!-- Loading Spinner (optional) 
        {#if isLoading}
            <div class="absolute top-1/2 right-4 transform -translate-y-1/2">
                <div
                    class="w-5 h-5 border-t-2 border-b-2 border-blue-500 rounded-full animate-spin"
                ></div>
            </div>
        {/if}
        -->
        <!-- Clear Button -->
        {#if searchQuery && !isLoading}
            <button
                on:click={clearSearch}
                class="absolute top-1/2 right-4 transform -translate-y-1/2 text-gray-500 hover:text-gray-800 dark:hover:text-gray-200"
                aria-label="Clear search"
            >
                <svg
                    xmlns="http://www.w3.org/2000/svg"
                    width="20"
                    height="20"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    stroke-width="2"
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    ><line x1="18" y1="6" x2="6" y2="18"></line><line
                        x1="6"
                        y1="6"
                        x2="18"
                        y2="18"
                    ></line></svg
                >
            </button>
        {/if}
    </div>

    <!-- Search Results Dropdown -->
    {#if results.length > 0 && isFocused}
        <div
            class="absolute z-10 w-full mt-4 bg-white border border-gray-200 rounded-lg shadow-xl"
        >
            <div class="">
                {#each results as page}
                    <a href={page.url} on:mousedown|preventDefault={() => navigateTo(page.url)} class="block p-4 hover:bg-gray-100">
                        <p class="font-semibold text-gray-800 text-left">
                            {page.title}
                        </p>
                    </a>
                {/each}
            </div>
        </div>
    {:else if searchQuery.length > 1 && results.length === 0 && isFocused}
        <div
            class="absolute z-10 w-full mt-2 bg-white border border-gray-200 rounded-lg shadow-xl p-4"
        >
            <p class="text-center text-gray-500">
                No results found for "{searchQuery}"
            </p>
        </div>
    {/if}
</div>
