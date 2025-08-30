<script>
    import { createEventDispatcher } from 'svelte';
    import { slide } from 'svelte/transition';

    export let title;
    let isOpen = false;
    const dispatch = createEventDispatcher();

    function toggle() {
        isOpen = !isOpen;
        dispatch('toggle', isOpen);
    }
</script>

<div class="my-4 rounded-lg overflow-hidden border border-gray-700">
    <button
        on:click={toggle}
        class="flex justify-between items-center w-full p-4 text-left font-medium text-lg bg-gray-700 hover:bg-gray-600 transition-colors duration-200"
    >
        <span>{title}</span>
        <svg
            class="w-6 h-6 transform transition-transform duration-200"
            class:rotate-90={isOpen}
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
        >
            <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M9 5l7 7-7 7"
            />
        </svg>
    </button>
    {#if isOpen}
        <div class="p-4" transition:slide>
            <slot />
        </div>
    {/if}
</div>
