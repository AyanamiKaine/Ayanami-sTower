<!-- src/components/ArtGallery.svelte -->
<!-- This Svelte component creates the interactive gallery. -->
<script>
    import { onMount, onDestroy } from 'svelte';

    // --- Artwork Data ---
    // In a real app, you might fetch this from a CMS or API.
    // For now, we'll define it here.
    // IMPORTANT: Replace these with paths to your own images in the `public/` folder.
    const artworks = [
        { id: 1, src: '/art/Charlemagne-1.webp', title: 'Charlemagne The Great', description: 'Based on my Ck2 campaign I had with him' },
        { id: 2, src: 'art/die_grafin-951x1536.png', title: 'Die Gräfin', description: 'Based on my DnD character' },
        { id: 3, src: 'art/study-painting-1422x1536.png', title: 'Study/Copy', description: '' },
        { id: 4, src: 'art/Illustration-55-1418x1536.webp', title: 'Illustration', description: '' },
        { id: 5, src: 'art/Portrait-study-1536x1436.png', title: 'Potrait', description: '' },

    ];

    // --- State Management ---
    let selectedArtwork = null; // This will hold the artwork object when the modal is open.
    let showModal = false;

    // --- Functions ---
    function openModal(artwork) {
        selectedArtwork = artwork;
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
        }, 300);
    }

    // --- Keyboard Accessibility ---
    function handleKeydown(event) {
        if (event.key === 'Escape' && selectedArtwork) {
            closeModal();
        }
    }

    function handleDialogKeydown(event) {
        // This allows closing the dialog by pressing Enter or Space on the backdrop,
        // which satisfies the accessibility warning.
        if (event.key === 'Enter' || event.key === ' ') {
            // We check if the event target is the dialog container itself
            // and not an element inside it (like the close button).
            if (event.currentTarget === event.target) {
                closeModal();
            }
        }
    }

    onMount(() => {
        window.addEventListener('keydown', handleKeydown);
    });

    onDestroy(() => {
        window.removeEventListener('keydown', handleKeydown);
        // Ensure scrolling is restored if component is destroyed while modal is open
        document.body.style.overflow = 'auto';
    });
</script>

<!-- Gallery Grid -->
<div class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6">
    {#each artworks as artwork (artwork.id)}
        <button 
            on:click={() => openModal(artwork)}
            class="group relative overflow-hidden rounded-lg shadow-lg transition-transform duration-300 ease-in-out hover:scale-105 hover:shadow-2xl focus:outline-none focus:ring-4 focus:ring-blue-500 focus:ring-opacity-50"
        >
            <img 
                src={artwork.src} 
                alt={artwork.title} 
                class="w-full h-full object-cover"
                loading="lazy"
            />
            <!-- CHANGE: Replaced the full overlay with a subtle gradient at the bottom that appears on hover. -->
            <div class="absolute inset-0 bg-gradient-to-t from-black/70 to-transparent opacity-0 group-hover:opacity-100 transition-opacity duration-300 flex items-end p-4">
                <h3 class="text-white text-lg font-bold opacity-0 group-hover:opacity-100 transform translate-y-4 group-hover:translate-y-0 transition-all duration-300">
                    {artwork.title}
                </h3>
            </div>
        </button>
    {/each}
</div>

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

        <!-- Modal Content -->
        <div 
            class="relative bg-white rounded-lg shadow-xl w-full max-w-4xl max-h-[90vh] flex flex-col md:flex-row overflow-hidden transition-transform duration-300"
            class:scale-95={!showModal}
            class:scale-100={showModal}
            on:click|stopPropagation
            role="document"
        >
            <div class="w-full md:w-2/3 h-64 md:h-auto flex-shrink-0">
                 <img 
                    src={selectedArtwork.src} 
                    alt={selectedArtwork.title} 
                    class="w-full h-full object-contain bg-gray-100"
                />
            </div>
            <div class="flex flex-col p-6 flex-grow">
                <h2 id="artwork-title" class="text-2xl font-bold text-gray-900 mb-2">{selectedArtwork.title}</h2>
                <p class="text-gray-700 flex-grow">{selectedArtwork.description}</p>
                <button 
                    on:click={closeModal}
                    class="mt-4 self-end bg-gray-800 text-white px-4 py-2 rounded-md hover:bg-gray-900 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-gray-800"
                    aria-label="Close artwork view"
                >
                    Close
                </button>
            </div>
        </div>

        <!-- Close button for accessibility and convenience -->
         <button
            on:click={closeModal}
            class="absolute top-4 right-4 text-white text-4xl leading-none hover:text-gray-300 focus:outline-none"
            aria-label="Close modal"
        >
            &times;
        </button>
    </div>
{/if}
