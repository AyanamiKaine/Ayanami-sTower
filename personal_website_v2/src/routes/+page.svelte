<script>
	
  import Quoteblock from "../lib/components/quoteblock.svelte";    
  import Header from "../lib/components/conditonHeader.svelte";
  import { onMount } from "svelte";
  import {slugify} from "../lib/util";

  // variables that hold the json response from CMS
  let blogPosts = [];
  let softwareEngineeringPosts = [];
  let algorithmPosts = [];
  
  onMount(async () => {
    try { 
      // Blog Posts CMS query for titles
      const response = await fetch("http://localhost:1337/api/blog-posts?fields=title");
      const blogData = await response.json(); 
      blogPosts = blogData.data; // Focus on the relevant data
    
      if(blogPosts == undefined){
        blogPosts = [];
      }

    } catch (error) {
      console.log(error);
      }
    
    try { 
      // Algorithm Posts CMS query for titles
      const response = await fetch("http://localhost:1337/api/algorithm-posts?fields=title");
      const algorithmData = await response.json(); 
      algorithmPosts = algorithmData.data; // Focus on the relevant data
      
      if(algorithmPosts == undefined){
        algorithmPosts = [];
      }

    } catch (error) {
      console.log(error);
    }
    
    try { 
      // Software Engineering CMS query for titles
      const response = await fetch("http://localhost:1337/api/software-engineering-posts?fields=title");
      const softwareEngineeringData = await response.json(); 
      softwareEngineeringPosts = softwareEngineeringData.data; // Focus on the relevant data
    
      if(softwareEngineeringPosts == undefined){
        softwareEngineeringPosts = [];
      }

    } catch (error) {
      console.log(error);
      }

  });
</script>

<div class="justify-center items-center">
  <h1 class="text-2xl font-bold">Ayanami's Tower</h1>

  <Quoteblock quote={"Our salvation lies not in knowing, but in creating!"} author={"Friedrich Nietzsche"} />

  <p class="text-lg my-4">Welcome to my Tower where I share my thoughts and teachings on programming, philosophy, art, and more.</p>

  <Header title="Blog Posts" condition={blogPosts.length != 0}>
    {#each blogPosts as post}
      <li class="text-blue-800"><a href="blog/{slugify(post.attributes.Title)}">{post.attributes.Title}</a></li>
    {/each}
  </Header>
  
  <Header title="Algorithms" condition={algorithmPosts.length != 0}>
  </Header>

  <Header title="Software Enginering" condition={softwareEngineeringPosts.length != 0}>
    {#each softwareEngineeringPosts as post}
      <li class="text-blue-800"><a href="software-engineering/{slugify(post.attributes.Title)}">{post.attributes.Title}</a></li>
    {/each}
  </Header>

  <p class="mt-2 text-sm text-gray-600">You might be supprised how much you can achive with just data structures and algorithms in a world where a new hot framework is just on the horizon.</p>

</div>
