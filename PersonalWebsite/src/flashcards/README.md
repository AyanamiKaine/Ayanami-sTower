AI - GENERATED README

# Flashcards Directory

This directory contains all your flashcard definitions for the spaced repetition app. Each file can contain one or more flashcards organized by topic.

## How to Add New Flashcards

### Create a New Flashcard File

1. Create a new `.astro` file in this directory (e.g., `my-topic.astro`)
2. Import the Flashcard component at the top
3. Define your flashcards

### Simple Text Flashcard

```astro
---
import Flashcard from "../components/Flashcard.astro";
---

<Flashcard
  id="unique-id-1"
  front="Your question here"
  back="Your answer here"
  tags={["category", "topic"]}
/>
```

### Rich HTML Flashcard

```astro
---
import Flashcard from "../components/Flashcard.astro";
import CodeBlock from "../components/CodeBlock.astro";
---

<Flashcard id="unique-id-2" tags={["programming"]}>
  <Fragment slot="front">
    <h3>What is a closure?</h3>
    <p>Explain with code examples.</p>
  </Fragment>
  <Fragment slot="back">
    <p>A closure is...</p>
    <CodeBlock lang="javascript" code={`function example() {
  let count = 0;
  return () => count++;
}`} />
  </Fragment>
</Flashcard>
```

## File Organization

Organize your flashcard files by topic:

```
flashcards/
├── fsrs-basics.astro       # Flashcards about FSRS
├── programming.astro        # Programming concepts
├── math-science.astro       # Math and science
├── history.astro            # History facts
├── stella-lang.astro        # Stella Lang specifics
└── your-topic.astro         # Add your own!
```

## Important Notes

1. **Unique IDs**: Each flashcard must have a unique `id` attribute
2. **Auto-Loading**: Files are automatically loaded when you start the dev server
3. **Syntax Highlighting**: Use the `CodeBlock` component for syntax-highlighted code
4. **Tags**: Use tags to categorize and filter flashcards (optional but recommended)

## Code Blocks with Syntax Highlighting

Import and use the `CodeBlock` component for beautiful syntax highlighting:

```astro
---
import Flashcard from "../components/Flashcard.astro";
import CodeBlock from "../components/CodeBlock.astro";
---

<Flashcard id="code-example" tags={["programming"]}>
  <Fragment slot="back">
    <CodeBlock lang="javascript" code={`function greet(name) {
  return \`Hello, \${name}!\`;
}`} />
  </Fragment>
</Flashcard>
```

Supported languages: `javascript`, `typescript`, `python`, `rust`, `go`, `java`, `cpp`, `html`, `css`, `json`, and many more!

You can also use plain `<pre><code>` blocks for simple code without syntax highlighting.

## Examples

### Multiple Cards in One File

```astro
---
import Flashcard from "../components/Flashcard.astro";
---

<Flashcard
  id="topic-1"
  front="Question 1?"
  back="Answer 1"
  tags={["category"]}
/>

<Flashcard
  id="topic-2"
  front="Question 2?"
  back="Answer 2"
  tags={["category"]}
/>
```

### Complex Formatting

```astro
<Flashcard id="advanced-1" tags={["advanced"]}>
  <Fragment slot="front">
    <h3>Main Question</h3>
    <ul>
      <li>Sub-question 1</li>
      <li>Sub-question 2</li>
    </ul>
  </Fragment>
  <Fragment slot="back">
    <h4>Answer</h4>
    <ol>
      <li>First point</li>
      <li>Second point</li>
    </ol>
    <blockquote>Important note here</blockquote>
  </Fragment>
</Flashcard>
```

## Tips

-   Keep flashcards focused on one concept
-   Use images, code blocks, and formatting to enhance learning
-   Group related flashcards in the same file
-   Use descriptive tags for better organization
-   Review and update flashcards as your understanding deepens
