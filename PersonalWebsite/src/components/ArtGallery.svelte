<!-- src/components/ArtGallery.svelte -->
<!-- This Svelte component creates the interactive gallery. -->
<script>
    import { onMount, onDestroy, tick } from 'svelte';

    // --- Artwork Data ---
    // This will be populated by fetching a JSON file.
    let allArtworks = [];
    
    const BATCH_SIZE = 12; // How many artworks to load at a time
    let visibleArtworks = [];
    let page = 0; // Start at page 0 to make slicing logic simpler

    // --- State Management ---
    let selectedArtwork = null; // This will hold the artwork object when the modal is open.
    let currentIndex = -1; // To track the current image index for navigation
    let showModal = false;

        // --- Functions ---
        function openModal(artwork) {
        // Find the global index of the clicked artwork to make navigation work
        const globalIndex = allArtworks.findIndex(a => a.id === artwork.id);
        if (globalIndex === -1) return;

        selectedArtwork = artwork;
        currentIndex = globalIndex;
        showModal = true;
        // Prevent background scrolling when modal is open
        document.body.style.overflow = 'hidden';
    }

    function closeModal() {
        showModal = false;
        // Allow background scrolling again
        document.body.style.overflow = 'auto';
        // We delay clearing the artwork to allow for the fade-out animation
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
        if (!selectedArtwork) return; // Do nothing if modal is closed

        if (event.key === 'Escape') {
            closeModal();
        }
        if (event.key === 'ArrowRight') {
            showNext();
        }
        if (event.key === 'ArrowLeft') {
            showPrevious();
        }
    }

    function handleDialogKeydown(event) {
        if (event.key === 'Enter' || event.key === ' ') {
            if (event.currentTarget === event.target) {
                closeModal();
            }
        }
    }

    // --- Lazy Loading Logic ---
    let sentinel; // This will be bound to the sentinel div at the bottom

    function loadMore() {
        // Check if there are more artworks to load
        if (allArtworks.length === 0 || visibleArtworks.length >= allArtworks.length) {
            return; // All loaded or not ready, do nothing
        }

        const nextPage = page + 1;
        const newArtworks = allArtworks.slice(page * BATCH_SIZE, nextPage * BATCH_SIZE);
        visibleArtworks = [...visibleArtworks, ...newArtworks];
        page = nextPage;
    }

    onMount(async () => {
        let observer; // Define observer in a broader scope for cleanup

        window.addEventListener('keydown', handleKeydown);

        try {
            const response = await fetch('/artworks.json');
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            const data = await response.json();
            
            allArtworks = data.map((art, index) => ({
                ...art,
                id: index + 1,
                src: `/art/${art.filename}`
            }));

            // --- Robust Initial Load ---
            // Load batches until the viewport is full or all items are loaded.
            do {
                loadMore();
                await tick(); // Wait for the DOM to update before checking scroll height.
            } while (
                visibleArtworks.length < allArtworks.length &&
                document.documentElement.scrollHeight <= document.documentElement.clientHeight
            );

            // --- Setup Intersection Observer for subsequent loads ---
            // This will now only handle loading when the user actually scrolls.
            observer = new IntersectionObserver(entries => {
                if (entries[0].isIntersecting) {
                    loadMore();
                }
            }, { 
                rootMargin: '0px 0px 300px 0px' 
            });

            if (sentinel) {
                observer.observe(sentinel);
            }

        } catch (error) {
            console.error("Could not fetch artworks.json:", error);
        }
        
        // Cleanup function
        return () => {
            window.removeEventListener('keydown', handleKeydown);
            if (sentinel && observer) {
                observer.unobserve(sentinel);
            }
        };
    });

    onDestroy(() => {
        document.body.style.overflow = 'auto';
    });
</script>

<!-- Gallery Grid -->
<div class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6">
    {#each visibleArtworks as artwork (artwork.id)}
        <button 
            on:click={() => openModal(artwork)}
            class="group relative overflow-hidden rounded-lg shadow-lg transition-transform duration-300 ease-in-out hover:scale-105 hover:shadow-2xl focus:outline-none focus:ring-opacity-50 h-80 cursor-pointer"
        >
            <img 
                src={artwork.src} 
                alt={artwork.title} 
                class="w-full h-full object-cover"
                loading="lazy"
            />
            <div class="absolute inset-0 bg-gradient-to-t from-black/70 to-transparent opacity-0 group-hover:opacity-100 transition-opacity duration-300 flex items-end justify-center p-4">
                <h3 class="text-white text-lg font-bold opacity-0 group-hover:opacity-100 transform translate-y-4 group-hover:translate-y-0 transition-all duration-300 text-center">
                    {artwork.title}
                </h3>
            </div>
        </button>
    {/each}
</div>

<!-- Sentinel Element for Intersection Observer -->
<!-- This invisible element triggers loading more items when it enters the screen -->
{#if allArtworks.length > 0 && visibleArtworks.length < allArtworks.length}
    <div bind:this={sentinel} class="h-1"></div>
{/if}


<!-- Modal for viewing a single artwork -->
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
        <!-- Modal Backdrop -->
        <div class="absolute inset-0 bg-black bg-opacity-60 backdrop-blur-sm"></div>

        <!-- Previous Button -->
        <button
            on:click|stopPropagation={showPrevious}
            class="absolute left-4 sm:left-8 top-1/2 -translate-y-1/2 z-[51] text-white bg-black bg-opacity-30 rounded-full p-2 hover:bg-opacity-50 transition-colors focus:outline-none focus:ring-2 focus:ring-white"
            aria-label="Previous image"
        >
            <svg xmlns="http://www.w3.org/2000/svg" class="h-8 w-8" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round" d="M15 19l-7-7 7-7" />
            </svg>
        </button>

        <!-- Modal Content -->
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
                class="block w-auto h-auto max-w-[80vw] sm:max-w-[90vw] max-h-[90vh] rounded-lg shadow-2xl"
            />
        </div>

        <!-- Next Button -->
        <button
            on:click|stopPropagation={showNext}
            class="absolute right-4 sm:right-8 top-1/2 -translate-y-1/2 z-[51] text-white bg-black bg-opacity-30 rounded-full p-2 hover:bg-opacity-50 transition-colors focus:outline-none focus:ring-2 focus:ring-white"
            aria-label="Next image"
        >
             <svg xmlns="http://www.w3.org/2000/svg" class="h-8 w-8" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round" d="M9 5l7 7-7 7" />
            </svg>
        </button>

        <!-- Close button for accessibility and convenience -->
         <button
            on:click={closeModal}
            class="absolute top-4 right-4 text-white text-4xl leading-none hover:text-gray-300 focus:outline-none z-[51]"
            aria-label="Close modal"
        >
            &times;
        </button>
    </div>
{/if}