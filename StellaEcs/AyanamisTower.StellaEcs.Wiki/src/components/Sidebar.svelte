<script>
	import { writable } from 'svelte/store';
	import { onMount } from 'svelte';

	export let navItems = [
		{
			title: 'Lore',
			path: '/wiki/lore',
			children: [
				{ title: 'Factions', path: '/wiki/lore/factions' },
				{ title: 'Timeline', path: '/wiki/lore/timeline' }
			]
		},
		{
			title: 'Engine',
			path: '/wiki/engine',
			children: [
				{ title: 'Getting Started', path: '/wiki/engine/getting-started' },
				{ title: 'Rendering', path: '/wiki/engine/rendering' }
			]
		}
	];

	const expandedItems = writable({});

	function toggleItem(itemTitle) {
		expandedItems.update((items) => {
			const newItems = {
				...items,
				[itemTitle]: !items[itemTitle]
			};
			localStorage.setItem('sidebar-expanded', JSON.stringify(newItems));
			return newItems;
		});
	}

	onMount(() => {
		const saved = localStorage.getItem('sidebar-expanded');
		if (saved) {
			expandedItems.set(JSON.parse(saved));
		}
	});
</script>

<div class="bg-gray-800 h-full p-4 text-gray-400 border-r border-gray-700">
	<div class="flex items-center space-x-2 mb-6">
		<span class="text-lg font-bold text-yellow-400">Stella Invicta Wiki</span>
	</div>
	<nav>
		<ul class="space-y-2">
			{#each navItems as item}
				<li>
					<a
						class="flex items-center justify-between p-2 rounded-lg hover:bg-gray-700 transition-colors duration-200"
						href={item.path}
						on:click|preventDefault={() => toggleItem(item.title)}
					>
						<span class="font-semibold text-gray-200">{item.title}</span>
						{#if item.children}
							<svg
								class="w-4 h-4 transform transition-transform duration-200"
								class:rotate-90={$expandedItems[item.title]}
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
						{/if}
					</a>
					{#if item.children && $expandedItems[item.title]}
						<ul class="pl-4 mt-2 space-y-1 border-l border-gray-600">
							{#each item.children as child}
								<li>
									<a
										class="block p-2 rounded-lg hover:bg-gray-700 transition-colors duration-200"
										href={child.path}>{child.title}</a
									>
								</li>
							{/each}
						</ul>
					{/if}
				</li>
			{/each}
		</ul>
	</nav>
</div>
