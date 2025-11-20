<!-- This Svelte component creates the interactive gallery with improved image loading. -->
<script>
    import { onMount, onDestroy, tick } from 'svelte';

    // --- Artwork Data ---
    let allArtworks = [];
    const BATCH_SIZE = 12;
    let visibleArtworks = [];
    let page = 0;

    // --- State Management ---
    let selectedArtwork = null;
    let currentIndex = -1;
    let showModal = false;

    // --- Image Loading State ---
    let loadedImages = {};

    /**
     * This function is called by the `on:load` event of each image.
     * It updates our tracking object and triggers Svelte's reactivity.
     * @param {string} src - The src of the image that just loaded.
     */
    function onImageLoad(src) {
        if (src) {
            loadedImages = { ...loadedImages, [src]: true };
        }
    }

    // --- Functions ---
    function openModal(artwork) {
        const globalIndex = allArtworks.findIndex(a => a.id === artwork.id);
        if (globalIndex === -1) return;

        selectedArtwork = artwork;
        currentIndex = globalIndex;
        showModal = true;
        document.body.style.overflow = 'hidden';
    }

    function closeModal() {
        showModal = false;
        document.body.style.overflow = 'auto';
        setTimeout(() => {
            selectedArtwork = null;
            currentIndex = -1;
        }, 300);
    }

    function showNext() {
        if (currentIndex === -1) return;
        const nextIndex = (currentIndex + 1) % allArtworks.length;
        currentIndex = nextIndex;
        selectedArtwork = allArtworks[nextIndex];
    }

    function showPrevious() {
        if (currentIndex === -1) return;
        const prevIndex = (currentIndex - 1 + allArtworks.length) % allArtworks.length;
        currentIndex = prevIndex;
        selectedArtwork = allArtworks[prevIndex];
    }

    // --- Keyboard Accessibility ---
    function handleKeydown(event) {
        if (!selectedArtwork) return;
        if (event.key === 'Escape') closeModal();
        if (event.key === 'ArrowRight') showNext();
        if (event.key === 'ArrowLeft') showPrevious();
    }

    function handleDialogKeydown(event) {
        if (event.key === 'Enter' || event.key === ' ') {
            if (event.currentTarget === event.target) closeModal();
        }
    }

    // --- Lazy Loading Logic ---
    let sentinel;

    function loadMore() {
        if (allArtworks.length === 0 || visibleArtworks.length >= allArtworks.length) return;
        const nextPage = page + 1;
        const newArtworks = allArtworks.slice(page * BATCH_SIZE, nextPage * BATCH_SIZE);
        visibleArtworks = [...visibleArtworks, ...newArtworks];
        page = nextPage;
    }

    onMount(async () => {
        let observer;
        window.addEventListener('keydown', handleKeydown);

        try {
            const response = await fetch('/artworks.json');
            if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
            
            const data = await response.json();
            allArtworks = data.map((art, index) => ({
                ...art,
                id: index + 1,
                src: `/art/${art.filename}`
            }));

            do {
                loadMore();
                await tick();
            } while (
                visibleArtworks.length < allArtworks.length &&
                document.documentElement.scrollHeight <= document.documentElement.clientHeight
            );

            observer = new IntersectionObserver(entries => {
                if (entries[0].isIntersecting) loadMore();
            }, { 
                rootMargin: '0px 0px 300px 0px' 
            });

            if (sentinel) observer.observe(sentinel);

        } catch (error) {
            console.error("Could not fetch artworks.json:", error);
        }
        
        return () => {
            window.removeEventListener('keydown', handleKeydown);
            if (sentinel && observer) observer.unobserve(sentinel);
        };
    });

    onDestroy(() => {
        document.body.style.overflow = 'auto';
    });
</script>

<!-- Gallery Grid -->
<div class="grid grid-cols-2 gap-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 sm:gap-6">
    {#each visibleArtworks as artwork (artwork.id)}
        {@const isLoaded = loadedImages[artwork.src]}
        <button 
            on:click={() => openModal(artwork)}
            class="group relative overflow-hidden rounded-sm sm:rounded-lg shadow-sm sm:shadow-lg transition-transform duration-300 ease-in-out hover:scale-105 hover:shadow-2xl focus:outline-none focus:ring-opacity-50 aspect-square sm:aspect-auto sm:h-80 cursor-pointer bg-gray-200 dark:bg-gray-800"
        >
            {#if !isLoaded}
                <div class="absolute inset-0 flex items-center justify-center">
                    <svg class="animate-spin h-8 w-8 text-gray-500 dark:text-gray-400" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                      <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                      <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                    </svg>
                </div>
            {/if}

            <!-- Actual Image: It's always in the DOM but invisible until loaded. -->
            <img 
                src={artwork.src} 
                alt={artwork.title} 
                class="absolute inset-0 w-full h-full object-cover transition-opacity duration-500"
                class:opacity-0={!isLoaded}
                class:opacity-100={isLoaded}
                on:load={() => onImageLoad(artwork.src)}
                loading="lazy"
            />
            <!-- Overlay with Title (unchanged) -->
            <div class="absolute inset-0 bg-gradient-to-t from-black/70 to-transparent opacity-0 group-hover:opacity-100 transition-opacity duration-300 flex items-end justify-center p-4">
                <h3 class="text-white text-lg font-bold opacity-0 group-hover:opacity-100 transform translate-y-4 group-hover:translate-y-0 transition-all duration-300 text-center">
                    {artwork.title}
                </h3>
            </div>
        </button>
    {/each}
</div>

<!-- Sentinel Element for Intersection Observer (unchanged) -->
{#if allArtworks.length > 0 && visibleArtworks.length < allArtworks.length}
    <div bind:this={sentinel} class="h-1"></div>
{/if}


<!-- Modal for viewing a single artwork (unchanged) -->
{#if selectedArtwork}
    <div 
        class="fixed inset-0 z-50 flex items-center justify-center p-4 transition-opacity duration-300"
        class:opacity-0={!showModal}
        class:opacity-100={showModal}
        role="dialog"
        aria-modal="true"
        aria-labelledby="artwork-title"
        on:click={closeModal}
        on:keydown={handleDialogKeydown}
        tabindex="-1"
    >
        <div class="absolute inset-0 bg-black bg-opacity-60 backdrop-blur-sm"></div>
        <button
            on:click|stopPropagation={showPrevious}
            class="absolute left-4 sm:left-8 top-1/2 -translate-y-1/2 z-[51] text-white bg-black bg-opacity-30 rounded-full p-2 hover:bg-opacity-50 transition-colors focus:outline-none focus:ring-2 focus:ring-white"
            aria-label="Previous image"
        >
            <svg xmlns="http://www.w3.org/2000/svg" class="h-8 w-8" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round" d="M15 19l-7-7 7-7" />
            </svg>
        </button>
        <div 
            class="relative transition-transform duration-300"
            class:scale-95={!showModal}
            class:scale-100={showModal}
            on:click|stopPropagation
            role="document"
        >
             <img 
                src={selectedArtwork.src} 
                alt={selectedArtwork.title} 
                class="block w-auto h-auto max-w-[95vw] sm:max-w-[90vw] max-h-[90vh] rounded-lg shadow-2xl"
            />
        </div>
        <button
            on:click|stopPropagation={showNext}
            class="absolute right-4 sm:right-8 top-1/2 -translate-y-1/2 z-[51] text-white bg-black bg-opacity-30 rounded-full p-2 hover:bg-opacity-50 transition-colors focus:outline-none focus:ring-2 focus:ring-white"
            aria-label="Next image"
        >
             <svg xmlns="http://www.w3.org/2000/svg" class="h-8 w-8" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round" d="M9 5l7 7-7 7" />
            </svg>
        </button>
         <button
            on:click={closeModal}
            class="absolute top-4 right-4 text-white text-4xl leading-none hover:text-gray-300 focus:outline-none z-[51]"
            aria-label="Close modal"
        >
            &times;
        </button>
    </div>
{/if}
