# Flashcard Organization Refactoring - Summary

## What Changed

The spaced repetition app has been refactored to use a **modular file-based approach** for flashcard definitions instead of having all flashcards defined inline in the main page.

## New Structure

### Before

All flashcards were defined directly in `StellaSpacedRepetition.astro`:

```astro
<Flashcard id="card-1" front="Q?" back="A!" />
<Flashcard id="card-2" front="Q?" back="A!" />
<!-- ... 50+ more cards ... -->
```

### After

Flashcards are organized in separate files in `src/flashcards/`:

```
src/flashcards/
├── README.md              # How to create flashcards
├── fsrs-basics.astro      # FSRS concept cards
├── programming.astro      # Programming cards
├── math-science.astro     # Math & science cards
├── history.astro          # History cards
└── stella-lang.astro      # Stella Lang cards
```

The main page now automatically imports all flashcard files:

```astro
const flashcardFiles = import.meta.glob('../../flashcards/*.astro', { eager: true });
```

## Benefits

1. **Better Organization**: Group related flashcards by topic
2. **Easy to Maintain**: Edit specific topics without touching other cards
3. **Scalable**: Add hundreds of flashcards without cluttering the main page
4. **Auto-Loading**: New flashcard files are automatically discovered
5. **Version Control**: Easier to track changes per topic

## How to Add New Flashcards

1. Create a new file in `src/flashcards/` (e.g., `physics.astro`)
2. Import the Flashcard component
3. Define your flashcards
4. Save the file
5. Restart the dev server (flashcards are loaded automatically)

## Example Flashcard File

```astro
---
import Flashcard from "../components/Flashcard.astro";
---

<Flashcard
  id="physics-1"
  front="What is Newton's First Law?"
  back="An object at rest stays at rest..."
  tags={["physics", "mechanics"]}
/>

<Flashcard
  id="physics-2"
  front="What is F = ma?"
  back="Force equals mass times acceleration"
  tags={["physics", "mechanics"]}
/>
```

## Files Created

-   `src/flashcards/README.md` - Guide for creating flashcards
-   `src/flashcards/fsrs-basics.astro` - FSRS basics flashcards
-   `src/flashcards/programming.astro` - Programming flashcards
-   `src/flashcards/math-science.astro` - Math & science flashcards
-   `src/flashcards/history.astro` - History flashcards
-   `src/flashcards/stella-lang.astro` - Stella Lang flashcards

## Files Modified

-   `src/pages/apps/StellaSpacedRepetition.astro` - Now uses `import.meta.glob()` to auto-load flashcards
-   `SPACED_REPETITION_README.md` - Updated documentation

## No Breaking Changes

The app works exactly the same from a user perspective:

-   Same review interface
-   Same FSRS scheduling
-   Same localStorage persistence
-   Same statistics tracking

Only the **organization** of flashcard definitions changed.
