<script>
	import Quoteblock from '../../../lib/components/quoteblock.svelte';

	export let data;
</script>


<div class="justify-center items-center">
    {#if data.data.data[0].attributes.Title}
        <h1 class="text-3xl font-bold">{data.data.data[0].attributes.Title}</h1>
    {/if}
    {#if data.data.data[0].attributes.Content}
    {#each data.data.data[0].attributes.Content as content}
            
    
    
    {#if content.type === "heading"}
    <h1 class="mt-2 text-2xl font-bold">
        {#each content.children as child}
            {child.text}
        {/each}
    </h1>
    {/if}
    
    
    {#if content.type === "paragraph"}
            <p class="mt-2">     
                {#each content.children as child}
                            {#if child.italic === true}
                                <i>{child.text}</i>
                            {:else if child.bold === true}
	                            <b>{child.text}</b>
                            {:else if child.type === "link"}
	                            <a class="text-blue-800" href="{child.url}">{child.children[0].text}</a>
                            {:else if child.underline === true}
	                            <u>{child.text}</u>
                            {:else if child.strikethrough === true}
	                            <s>{child.text}</s>
                            {:else}
                                {child.text}
                            {/if}
                    
                {/each}
            </p>
            {/if}
            {#if content.type === "quote"}
                <blockquote>
                    {#each content.children as child}
                    <Quoteblock quote={child.text} author={"Ayanami"} />

                    {/each}
                </blockquote>
            {/if}
            {#if content.type === "list"}
            <h1 class="mt-2">
                <ol class="list-decimal">
                {#each content.children as child}
                    {#if child.children[0].text}
                        <li class="mt-2">{child.children[0].text}</li>
                    {/if}  
                {/each}
            </ol>  
            </h1>
            {/if}
    {/each}
    {/if}
    </div>