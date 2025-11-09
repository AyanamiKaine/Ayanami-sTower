# Spaced Repetition App

A personal spaced repetition learning system built with Astro, Svelte, and the FSRS (Free Spaced Repetition Scheduler) algorithm.

## Features

-   **FSRS Algorithm**: Uses the modern FSRS algorithm for optimal learning intervals
-   **Priority Queue**: Automatically prioritizes cards based on due dates
-   **Persistent Progress**: All progress is saved to localStorage
-   **Cram Mode**: Review all cards without updating scheduling - perfect for exam prep
-   **Modular Flashcards**: Organize flashcards in separate files by topic
-   **Auto-Loading**: Flashcards are automatically loaded from the `src/flashcards/` directory
-   **Flexible Card Definitions**: Create flashcards with simple text or rich HTML content
-   **Statistics Dashboard**: Track your learning progress with real-time stats
-   **Customizable**: Define cards in separate files for easy organization

## How to Use

### Installing Dependencies

First, install the required package:

```bash
npm install ts-fsrs
```

### Creating Flashcards

Create new flashcard files in the `src/flashcards/` directory. Each file can contain multiple flashcards organized by topic.

#### Create a New Flashcard File

1. Create a new `.astro` file in `src/flashcards/` (e.g., `my-topic.astro`)
2. Import the Flashcard component
3. Define your flashcards

Example file `src/flashcards/my-topic.astro`:

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

<Flashcard id="unique-id-2" tags={["programming"]}>
  <Fragment slot="front">
    <h3>What is a closure?</h3>
    <p>Explain with code examples.</p>
  </Fragment>
  <Fragment slot="back">
    <p>A closure is...</p>
    <pre><code>{`function example() {
  let count = 0;
  return () => count++;
}`}</code></pre>
  </Fragment>
</Flashcard>
```

**Important**: Wrap code blocks with curly braces in template literals: `` {`code here`} ``

### Review Modes

#### Normal Mode (Default)

Reviews cards that are due and updates their scheduling based on FSRS algorithm.

When reviewing cards, you'll see four rating options:

-   **Again** (Red): You forgot the card completely → Review very soon (< 10 minutes)
-   **Hard** (Blue): You remembered with difficulty → Review sooner than normal
-   **Good** (Green): You remembered correctly → Review at the standard interval
-   **Easy** (Yellow): You remembered easily → Review much later

The FSRS algorithm automatically calculates optimal intervals based on your ratings.

#### Cram Mode

Click the **"📖 Cram Mode"** button to enter cram mode. This mode is perfect for:

-   **Exam preparation**: Review all cards regardless of due date
-   **Quick refreshers**: Go through your entire deck without consequences
-   **Practice sessions**: Test your knowledge without affecting your learning schedule

**In Cram Mode:**

-   ✅ All cards are loaded and shuffled for variety
-   ✅ You can still rate cards (Again/Hard/Good/Easy) for self-assessment
-   ✅ Progress tracker shows how many cards you've reviewed
-   ❌ **Card scheduling is NOT updated** - your progress is preserved
-   ❌ Review history is not recorded

Click **"📚 Exit Cram"** to return to normal spaced repetition mode.

#### Manage Mode

Click the **"📋 Manage Cards"** button to view all your flashcards in a sortable table.

**Features:**

-   📊 **View all cards** in a comprehensive table
-   🔍 **Search** by ID, content, or tags
-   ⬆️⬇️ **Sort** by any column (click column headers)
-   🎨 **Color-coded states** (New, Learning, Review, Relearning)
-   🔴 **Highlight overdue cards** for easy identification
-   📈 **View statistics** (difficulty, stability, review count)

**Sortable Columns:**

-   **ID**: Card identifier
-   **Front**: Preview of question (first 50 characters)
-   **Tags**: Category labels
-   **Due Date**: When the card is next scheduled
-   **State**: Current learning phase
-   **Difficulty**: Card complexity (0-10)
-   **Stability**: Days until 90% recall probability
-   **Reviews**: Total number of reviews

Click **"🎴 Review Mode"** to return to the review interface.

## File Structure

```
src/
├── components/
│   ├── Flashcard.astro          # Component for defining flashcards
│   └── SpacedRepetition.svelte  # Main review interface
├── flashcards/                   # Directory for flashcard files
│   ├── README.md                # Guide for creating flashcards
│   ├── fsrs-basics.astro        # Example: FSRS concept flashcards
│   ├── programming.astro        # Example: Programming flashcards
│   ├── math-science.astro       # Example: Math & science flashcards
│   └── your-topic.astro         # Add your own flashcard files here!
├── lib/
│   └── priorityQueue.ts         # Priority queue implementation
└── pages/
    └── apps/
        └── StellaSpacedRepetition.astro  # Main page (auto-loads flashcards)
```

## Components

### Flashcard.astro

Defines individual flashcards. Accepts:

-   `id` (required): Unique identifier for the card
-   `front` (optional): Front content as text
-   `back` (optional): Back content as text
-   `tags` (optional): Array of tags for categorization

Can also use slots for rich HTML content:

-   `slot="front"`: Custom HTML for the question
-   `slot="back"`: Custom HTML for the answer

### SpacedRepetition.svelte

The main review interface that:

-   Loads flashcards from the page
-   Manages the priority queue
-   Handles FSRS scheduling
-   Saves/loads progress from localStorage
-   Displays statistics and review interface

### priorityQueue.ts

A min-heap-based priority queue that:

-   Orders cards by due date
-   Efficiently retrieves the next card to review
-   Supports updating card priorities

## Statistics

The app tracks:

-   **Total**: Total number of flashcards
-   **Due**: Cards currently due for review
-   **New**: Cards never reviewed
-   **Learning**: Cards in the initial learning phase
-   **Review**: Cards in long-term review

## Card Metadata

Each card displays:

-   **Difficulty**: How hard the card is (0-10)
-   **Stability**: How many days until 90% recall probability
-   **Reviews**: Total number of times reviewed
-   **Tags**: Custom tags for organization

## Data Persistence

All progress is automatically saved to `localStorage` under the key `spaced-repetition-data`.

To reset progress, click the "Reset Progress" button in the stats bar.

## Customization

### Adding More Cards

Create a new `.astro` file in the `src/flashcards/` directory with your flashcards. They will be automatically loaded when you restart the dev server. Each card needs a unique `id`.

Example: Create `src/flashcards/chemistry.astro`:

```astro
---
import Flashcard from "../components/Flashcard.astro";
---

<Flashcard
  id="chem-1"
  front="What is H2O?"
  back="Water"
  tags={["chemistry", "basics"]}
/>
```

### Styling

Modify the styles in:

-   `SpacedRepetition.svelte` for the review interface
-   `StellaSpacedRepetition.astro` for the page layout

### FSRS Parameters

In `SpacedRepetition.svelte`, you can customize FSRS parameters:

```javascript
import { generatorParameters, FSRSParameters } from "ts-fsrs";

const params = generatorParameters({
    maximum_interval: 365, // Max days between reviews
    request_retention: 0.9, // Target retention rate
    // ... other parameters
});

let fsrs = $state(new FSRS(params));
```

## Tips for Effective Learning

1. **Review regularly**: Come back daily to review due cards
2. **Be honest**: Rate cards accurately based on recall difficulty
3. **Use tags**: Organize cards by topic for better context
4. **Rich content**: Use HTML slots for code, images, or formatted content
5. **Small chunks**: Break complex topics into multiple simple cards
6. **Use cram mode wisely**: Before exams or when you need a refresher, but don't rely on it for long-term learning
7. **Normal mode for retention**: Use regular spaced repetition mode to build long-term memory

## Development

Start the dev server:

```bash
npm run dev
```

Visit `http://localhost:4321/apps/StellaSpacedRepetition` to use the app.

## License

Personal use project - customize as needed!
