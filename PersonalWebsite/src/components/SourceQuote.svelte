<script lang="ts">
  export let quote: string;
  export let author: string;
  export let source: string;
  export let page: number | string = "";
  export let chapter: string = "";
  export let url: string = "";
</script>

<figure class="source-quote">
  <blockquote cite={url || ""}>
    <p>{quote}</p>
  </blockquote>
  
  <figcaption>
    <span class="citation">
      {#if page}
        <span class="page">p. {page}</span>
      {/if}
      {#if chapter}
        <span class="chapter">{chapter}</span>
      {/if}
      <cite>{source}</cite> — <span class="author">{author}</span>
    </span>
    
    {#if url}
      <a href={url} class="source-url" target="_blank" rel="noopener noreferrer" title="View source">🔗</a>
    {/if}
  </figcaption>
</figure>

<style>
  .source-quote {
    margin: 1.5rem 0;
    padding: 0;
    border: none;
  }

  blockquote {
    margin: 0 0 0.5rem 0;
    padding: 0.75rem 0 0.75rem 1.25rem;
    border-left: 3px solid #6366f1;
    background: linear-gradient(90deg, rgba(99, 102, 241, 0.04) 0%, transparent 100%);
  }

  blockquote p {
    margin: 0;
    font-size: 1rem;
    font-style: italic;
    color: #1f2937;
    line-height: 1.6;
  }

  figcaption {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    justify-content: space-between;
    font-size: 0.9rem;
    color: #475569;
  }

  .citation {
    display: flex;
    gap: 0.5rem;
    align-items: center;
    flex-wrap: wrap;
  }

  .page,
  .chapter {
    font-size: 0.85rem;
    background: #f3f4f6;
    color: #1f2937;
    padding: 0.2rem 0.5rem;
    border-radius: 3px;
    border: 1px solid #d1d5db;
    font-weight: 500;
  }

  cite {
    font-style: italic;
    font-weight: 600;
    color: #1f2937;
  }

  .author {
    font-weight: 600;
    color: #6366f1;
  }

  .source-url {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 24px;
    height: 24px;
    border-radius: 4px;
    text-decoration: none;
    background: rgba(99, 102, 241, 0.08);
    transition: background-color 120ms ease, transform 120ms ease;
  }

  .source-url:hover,
  .source-url:focus {
    background: rgba(99, 102, 241, 0.16);
    transform: translateY(-2px);
    outline: none;
  }


  /* Responsive: stack citation on narrow screens */
  @media (max-width: 480px) {
    figcaption {
      flex-direction: column;
      align-items: flex-start;
      gap: 0.5rem;
    }

    .citation {
      width: 100%;
    }
  }
</style>
