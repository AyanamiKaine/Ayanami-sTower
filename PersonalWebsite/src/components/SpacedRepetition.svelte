<script>
  import { onMount } from 'svelte';
  import { createEmptyCard, FSRS, Rating } from 'ts-fsrs';
  import { PriorityQueue } from '../lib/priorityQueue';

  /**
   * SpacedRepetition Component
   * 
   * Main component for the spaced repetition system.
   * Uses FSRS algorithm and a priority queue to manage flashcard reviews.
   */

  const STORAGE_KEY = 'spaced-repetition-data';
  
  let queue = $state(new PriorityQueue());
  // Initialize FSRS with fuzzing enabled
  let fsrs = $state(new FSRS({
    enable_fuzz: true
  }));
  let currentCard = $state(null);
  let showAnswer = $state(false);
  let showReferences = $state(false);
  let cramMode = $state(false);
  let cramCards = $state([]);
  let cramIndex = $state(0);
  let cramAgainCards = $state([]); // Cards marked as "Again" to review again
  let viewMode = $state('review'); // 'review' or 'manage'
  let sortColumn = $state('due');
  let sortDirection = $state('asc');
  let searchQuery = $state('');
  let stats = $state({
    total: 0,
    due: 0,
    new: 0,
    learning: 0,
    review: 0
  });

  onMount(() => {
    initializeCards();
  });

  /**
   * Initialize flashcards from the HTML definitions
   */
  function initializeCards() {
    const cardElements = document.querySelectorAll('.flashcard-definition');
    const existingData = loadFromStorage();
    
    cardElements.forEach(el => {
      const id = el.dataset.id;
      const tags = JSON.parse(el.dataset.tags || '[]');
      const references = JSON.parse(el.dataset.references || '[]');
      let source = el.dataset.source || '';
      
      // If no source is provided, try to get it from the parent wrapper
      if (!source) {
        const wrapper = el.closest('[data-flashcard-file]');
        if (wrapper) {
          source = wrapper.dataset.flashcardFile || '';
        }
      }
      
      // Get front content
      let front = '';
      const frontSimple = el.querySelector('.front-simple');
      const frontSlot = el.querySelector('.front-slot');
      if (frontSimple && frontSimple.dataset.content) {
        front = frontSimple.dataset.content;
      } else if (frontSlot && frontSlot.innerHTML.trim()) {
        front = frontSlot.innerHTML;
      }
      
      // Get back content
      let back = '';
      const backSimple = el.querySelector('.back-simple');
      const backSlot = el.querySelector('.back-slot');
      if (backSimple && backSimple.dataset.content) {
        back = backSimple.dataset.content;
      } else if (backSlot && backSlot.innerHTML.trim()) {
        back = backSlot.innerHTML;
      }
      
      // Check if we have existing progress for this card
      const existingCard = existingData?.find(c => c.id === id);
      
      let fsrsCard;
      let due;
      
      if (existingCard && existingCard.card) {
        // Restore existing card
        fsrsCard = existingCard.card;
        due = new Date(existingCard.due);
      } else {
        // Create new card
        fsrsCard = createEmptyCard();
        due = new Date(); // New cards are due immediately
      }
      
      const cardData = {
        id,
        front,
        back,
        tags,
        card: fsrsCard,
        due,
        source,
        references
      };
      
      queue.enqueue(cardData);
    });
    
    updateStats();
    loadNextCard();
  }

  /**
   * Load the next card due for review
   */
  function loadNextCard() {
    // Reset UI state when loading new card
    showAnswer = false;
    showReferences = false;
    
    if (cramMode) {
      // In cram mode: first check if we have cards in the main deck
      if (cramIndex < cramCards.length) {
        currentCard = cramCards[cramIndex];
      } else if (cramAgainCards.length > 0) {
        // Main deck is done, now review "Again" cards
        currentCard = cramAgainCards.shift();
      } else {
        // Everything is done
        currentCard = null;
      }
    } else {
      // Normal mode: only load due cards
      const dueCards = queue.getDueCards();
      if (dueCards.length > 0) {
        currentCard = queue.peek();
      } else {
        currentCard = null;
      }
    }
  }
  
  /**
   * Toggle cram mode
   */
  function toggleCramMode() {
    cramMode = !cramMode;
    
    if (cramMode) {
      // Entering cram mode: load all cards
      cramCards = queue.getAllCards();
      cramIndex = 0;
      cramAgainCards = [];
      // Shuffle the cards for variety
      shuffleArray(cramCards);
    } else {
      // Exiting cram mode: reset to normal
      cramCards = [];
      cramIndex = 0;
      cramAgainCards = [];
    }
    
    loadNextCard();
  }
  
  /**
   * Shuffle array in place (Fisher-Yates algorithm)
   */
  function shuffleArray(array) {
    for (let i = array.length - 1; i > 0; i--) {
      const j = Math.floor(Math.random() * (i + 1));
      [array[i], array[j]] = [array[j], array[i]];
    }
  }

  /**
   * Handle rating selection
   */
  function handleRating(rating) {
    if (!currentCard) return;
    
    if (cramMode) {
      // In cram mode: don't update FSRS scheduling
      if (rating === 1) {
        // Rating is "Again" - add card to the end of the "Again" pile to review later
        cramAgainCards.push(currentCard);
        // Don't increment cramIndex - this card doesn't count as "completed"
      } else {
        // Card was reviewed successfully (Hard/Good/Easy) - move to next card
        cramIndex++;
      }
      loadNextCard();
    } else {
      // Normal mode: update FSRS scheduling
      const now = new Date();
      const scheduling = fsrs.repeat(currentCard.card, now);
      const ratingKey = getRatingKey(rating);
      const scheduledCard = scheduling[ratingKey];
      
      // Update the card with new FSRS data
      const updatedCard = {
        ...currentCard,
        card: scheduledCard.card,
        due: scheduledCard.card.due
      };
      
      // Remove current card and add updated one
      queue.dequeue();
      queue.enqueue(updatedCard);
      
      // Save progress
      saveProgress();
      updateStats();
      
      // Load next card
      loadNextCard();
    }
  }

  /**
   * Convert rating number to Rating enum key
   */
  function getRatingKey(rating) {
    switch(rating) {
      case 1: return Rating.Again;
      case 2: return Rating.Hard;
      case 3: return Rating.Good;
      case 4: return Rating.Easy;
      default: return Rating.Good;
    }
  }

  /**
   * Toggle answer visibility
   */
  function toggleAnswer() {
    showAnswer = !showAnswer;
  }

  /**
   * Toggle references visibility
   */
  function toggleReferences() {
    showReferences = !showReferences;
  }

  /**
   * Update statistics
   */
  function updateStats() {
    const allCards = queue.getAllCards();
    const now = new Date();
    
    stats.total = allCards.length;
    stats.due = allCards.filter(c => c.due <= now).length;
    stats.new = allCards.filter(c => c.card.state === 0).length; // State.New = 0
    stats.learning = allCards.filter(c => c.card.state === 1 || c.card.state === 3).length; // Learning or Relearning
    stats.review = allCards.filter(c => c.card.state === 2).length; // State.Review = 2
  }

  /**
   * Save progress to localStorage
   */
  function saveProgress() {
    const data = queue.getAllCards().map(card => ({
      id: card.id,
      card: card.card,
      due: card.due.toISOString(),
      front: card.front,
      back: card.back,
      tags: card.tags,
      source: card.source,
      references: card.references
    }));
    
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(data));
    } catch (e) {
      console.error('Failed to save progress:', e);
    }
  }

  /**
   * Load progress from localStorage
   */
  function loadProgress() {
    const data = loadFromStorage();
    if (data && data.length > 0) {
      queue.loadCards(data);
      updateStats();
      loadNextCard();
    }
  }

  /**
   * Load data from localStorage
   */
  function loadFromStorage() {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (stored) {
        return JSON.parse(stored);
      }
    } catch (e) {
      console.error('Failed to load progress:', e);
    }
    return null;
  }

  /**
   * Reset all progress
   */
  function resetProgress() {
    if (confirm('Are you sure you want to reset all progress? This cannot be undone.')) {
      localStorage.removeItem(STORAGE_KEY);
      queue = new PriorityQueue();
      currentCard = null;
      showAnswer = false;
      initializeCards();
    }
  }

  /**
   * Format date for display
   */
  function formatDate(date) {
    if (!date) return 'N/A';
    const d = new Date(date);
    const now = new Date();
    const diff = Math.floor((d - now) / (1000 * 60 * 60 * 24));
    
    if (diff < 0) return 'Now';
    if (diff === 0) return 'Today';
    if (diff === 1) return 'Tomorrow';
    if (diff < 7) return `${diff} days`;
    if (diff < 30) return `${Math.floor(diff / 7)} weeks`;
    if (diff < 365) return `${Math.floor(diff / 30)} months`;
    return `${Math.floor(diff / 365)} years`;
  }

  /**
   * Format date for table display
   */
  function formatDateFull(date) {
    if (!date) return 'N/A';
    const d = new Date(date);
    return d.toLocaleString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  /**
   * Toggle view mode between review and manage
   */
  function toggleViewMode() {
    viewMode = viewMode === 'review' ? 'manage' : 'review';
  }

  /**
   * Sort table by column
   */
  function sortBy(column) {
    if (sortColumn === column) {
      sortDirection = sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      sortColumn = column;
      sortDirection = 'asc';
    }
  }

  /**
   * Get sorted and filtered cards for table
   */
  function getSortedCards() {
    let cards = queue.getAllCards();
    
    // Filter by search query
    if (searchQuery.trim()) {
      const query = searchQuery.toLowerCase();
      cards = cards.filter(card => {
        const frontText = stripHtml(card.front).toLowerCase();
        const backText = stripHtml(card.back).toLowerCase();
        const tagsText = card.tags.join(' ').toLowerCase();
        return frontText.includes(query) || 
               backText.includes(query) || 
               tagsText.includes(query) ||
               card.id.toLowerCase().includes(query);
      });
    }
    
    // Sort cards
    cards.sort((a, b) => {
      let comparison = 0;
      
      switch(sortColumn) {
        case 'id':
          comparison = a.id.localeCompare(b.id);
          break;
        case 'front':
          comparison = stripHtml(a.front).localeCompare(stripHtml(b.front));
          break;
        case 'tags':
          comparison = a.tags.join(',').localeCompare(b.tags.join(','));
          break;
        case 'due':
          comparison = new Date(a.due) - new Date(b.due);
          break;
        case 'state':
          comparison = a.card.state - b.card.state;
          break;
        case 'difficulty':
          comparison = a.card.difficulty - b.card.difficulty;
          break;
        case 'stability':
          comparison = a.card.stability - b.card.stability;
          break;
        case 'reps':
          comparison = a.card.reps - b.card.reps;
          break;
      }
      
      return sortDirection === 'asc' ? comparison : -comparison;
    });
    
    return cards;
  }

  /**
   * Strip HTML tags from string
   */
  function stripHtml(html) {
    const tmp = document.createElement('div');
    tmp.innerHTML = html;
    return tmp.textContent || tmp.innerText || '';
  }

  /**
   * Get state name
   */
  function getStateName(state) {
    const states = ['New', 'Learning', 'Review', 'Relearning'];
    return states[state] || 'Unknown';
  }
</script>

<div class="spaced-repetition-container">
  <!-- Stats Bar -->
  <div class="stats-bar">
    <div class="stat">
      <span class="stat-label">Total:</span>
      <span class="stat-value">{stats.total}</span>
    </div>
    <div class="stat due">
      <span class="stat-label">Due:</span>
      <span class="stat-value">{stats.due}</span>
    </div>
    <div class="stat new">
      <span class="stat-label">New:</span>
      <span class="stat-value">{stats.new}</span>
    </div>
    <div class="stat learning">
      <span class="stat-label">Learning:</span>
      <span class="stat-value">{stats.learning}</span>
    </div>
    <div class="stat review">
      <span class="stat-label">Review:</span>
      <span class="stat-value">{stats.review}</span>
    </div>
    {#if cramMode}
      <div class="stat cram">
        <span class="stat-label">Cram Progress:</span>
        <span class="stat-value">{cramIndex} / {cramCards.length}</span>
      </div>
      {#if cramAgainCards.length > 0}
        <div class="stat again">
          <span class="stat-label">Review Again:</span>
          <span class="stat-value">{cramAgainCards.length}</span>
        </div>
      {/if}
    {/if}
    <button 
      class="view-toggle-btn {viewMode === 'manage' ? 'active' : ''}" 
      onclick={toggleViewMode}
      title={viewMode === 'review' ? "View all flashcards" : "Return to review mode"}
    >
      {viewMode === 'review' ? '📋 Manage Cards' : '🎴 Review Mode'}
    </button>
    {#if viewMode === 'review'}
      <button 
        class="mode-btn {cramMode ? 'active' : ''}" 
        onclick={toggleCramMode}
        title={cramMode ? "Exit Cram Mode" : "Enter Cram Mode - Review all cards without updating scheduling"}
      >
        {cramMode ? '📚 Exit Cram' : '📖 Cram Mode'}
      </button>
    {/if}
    <button class="reset-btn" onclick={resetProgress}>Reset Progress</button>
  </div>

  {#if viewMode === 'manage'}
    <!-- Manage View - Table of all cards -->
    <div class="manage-view">
      <div class="table-controls">
        <input 
          type="text" 
          class="search-input" 
          placeholder="🔍 Search cards by ID, content, or tags..."
          bind:value={searchQuery}
        />
        <div class="table-info">
          Showing {getSortedCards().length} of {stats.total} cards
        </div>
      </div>

      <div class="table-container">
        <table class="flashcard-table">
          <thead>
            <tr>
              <th onclick={() => sortBy('front')} class="sortable">
                Front {sortColumn === 'front' ? (sortDirection === 'asc' ? '▲' : '▼') : ''}
              </th>
              <th onclick={() => sortBy('tags')} class="sortable">
                Tags {sortColumn === 'tags' ? (sortDirection === 'asc' ? '▲' : '▼') : ''}
              </th>
              <th onclick={() => sortBy('due')} class="sortable">
                Due Date {sortColumn === 'due' ? (sortDirection === 'asc' ? '▲' : '▼') : ''}
              </th>
              <th onclick={() => sortBy('state')} class="sortable">
                State {sortColumn === 'state' ? (sortDirection === 'asc' ? '▲' : '▼') : ''}
              </th>
              <th onclick={() => sortBy('difficulty')} class="sortable">
                Difficulty {sortColumn === 'difficulty' ? (sortDirection === 'asc' ? '▲' : '▼') : ''}
              </th>
              <th onclick={() => sortBy('stability')} class="sortable">
                Stability {sortColumn === 'stability' ? (sortDirection === 'asc' ? '▲' : '▼') : ''}
              </th>
              <th onclick={() => sortBy('reps')} class="sortable">
                Reviews {sortColumn === 'reps' ? (sortDirection === 'asc' ? '▲' : '▼') : ''}
              </th>
            </tr>
          </thead>
          <tbody>
            {#each getSortedCards() as card}
              <tr class="card-row {new Date(card.due) <= new Date() ? 'due-now' : ''}">
                <td class="cell-front" title={stripHtml(card.front)}>
                  {stripHtml(card.front).substring(0, 50)}{stripHtml(card.front).length > 50 ? '...' : ''}
                </td>
                <td class="cell-tags">
                  {#each card.tags as tag}
                    <span class="tag-badge">{tag}</span>
                  {/each}
                </td>
                <td class="cell-due">
                  <span class="due-badge {new Date(card.due) <= new Date() ? 'overdue' : ''}">
                    {formatDateFull(card.due)}
                  </span>
                </td>
                <td class="cell-state">
                  <span class="state-badge state-{card.card.state}">
                    {getStateName(card.card.state)}
                  </span>
                </td>
                <td class="cell-difficulty">{card.card.difficulty.toFixed(2)}</td>
                <td class="cell-stability">{card.card.stability.toFixed(2)} days</td>
                <td class="cell-reps">{card.card.reps}</td>
              </tr>
            {/each}
          </tbody>
        </table>
      </div>
    </div>
  {:else}
    <!-- Review View - Card Display -->
    {#if currentCard}
    <div class="card-container">
      <div class="card">
        <div class="card-front">
          <div class="card-label">Question</div>
          <div class="card-content">
            {@html currentCard.front}
          </div>
        </div>

        {#if showAnswer}
          <div class="card-back">
            <div class="card-label">Answer</div>
            <div class="card-content">
              {@html currentCard.back}
            </div>
          </div>

          <!-- Rating Buttons -->
          {#if cramMode}
            <div class="cram-notice">
              ⚠️ Cram Mode: Reviews won't affect scheduling
            </div>
            <div class="rating-buttons cram-mode">
              <button class="rating-btn again" onclick={() => handleRating(1)}>
                <span class="rating-label">Again</span>
                <span class="rating-time">Next →</span>
              </button>
              <button class="rating-btn hard" onclick={() => handleRating(2)}>
                <span class="rating-label">Hard</span>
                <span class="rating-time">Next →</span>
              </button>
              <button class="rating-btn good" onclick={() => handleRating(3)}>
                <span class="rating-label">Good</span>
                <span class="rating-time">Next →</span>
              </button>
              <button class="rating-btn easy" onclick={() => handleRating(4)}>
                <span class="rating-label">Easy</span>
                <span class="rating-time">Next →</span>
              </button>
            </div>
          {:else}
            <div class="rating-buttons">
              <button class="rating-btn again" onclick={() => handleRating(1)}>
                <span class="rating-label">Again</span>
                <span class="rating-time">&lt;10m</span>
              </button>
              <button class="rating-btn hard" onclick={() => handleRating(2)}>
                <span class="rating-label">Hard</span>
                <span class="rating-time">{formatDate(fsrs.repeat(currentCard.card, new Date())[Rating.Hard].card.due)}</span>
              </button>
              <button class="rating-btn good" onclick={() => handleRating(3)}>
                <span class="rating-label">Good</span>
                <span class="rating-time">{formatDate(fsrs.repeat(currentCard.card, new Date())[Rating.Good].card.due)}</span>
              </button>
              <button class="rating-btn easy" onclick={() => handleRating(4)}>
                <span class="rating-label">Easy</span>
                <span class="rating-time">{formatDate(fsrs.repeat(currentCard.card, new Date())[Rating.Easy].card.due)}</span>
              </button>
            </div>
          {/if}
        {:else}
          <button class="show-answer-btn" onclick={toggleAnswer}>
            Show Answer
          </button>
        {/if}

        <!-- Card Metadata -->
        <div class="card-metadata">
          {#if currentCard.tags && currentCard.tags.length > 0}
            <div class="tags">
              {#each currentCard.tags as tag}
                <span class="tag">{tag}</span>
              {/each}
            </div>
          {/if}
          <div class="card-info">
            <span>Difficulty: {currentCard.card.difficulty.toFixed(2)}</span>
            <span>Stability: {currentCard.card.stability.toFixed(2)} days</span>
            <span>Reviews: {currentCard.card.reps}</span>
            {#if currentCard.source}
              <a 
                href={`https://github.com/AyanamiKaine/Ayanami-sTower/tree/main/PersonalWebsite/src/flashcards/${currentCard.source}`}
                target="_blank"
                rel="noopener noreferrer"
                class="edit-btn"
                title="Edit this flashcard on GitHub"
              >
                ✏️ Edit
              </a>
            {/if}
          </div>

          <!-- References Section -->
          {#if currentCard.references && currentCard.references.length > 0}
            <div class="references-section">
              <button class="references-toggle" onclick={toggleReferences}>
                📚 References ({currentCard.references.length})
                <span class="toggle-icon">{showReferences ? '▼' : '▶'}</span>
              </button>
              {#if showReferences}
                <div class="references-content">
                  <ul class="references-list">
                    {#each currentCard.references as reference}
                      <li class="reference-item">
                        {#if reference.startsWith('http://') || reference.startsWith('https://')}
                          <a href={reference} target="_blank" rel="noopener noreferrer" class="reference-link">
                            🔗 {reference}
                          </a>
                        {:else}
                          <span class="reference-text">📖 {reference}</span>
                        {/if}
                      </li>
                    {/each}
                  </ul>
                </div>
              {/if}
            </div>
          {/if}
        </div>
      </div>
    </div>
    {:else}
      <div class="no-cards">
        <h2>🎉 All done!</h2>
        <p>No cards due for review right now.</p>
        {#if stats.total > 0}
          <p class="next-review">Come back later for your next review session.</p>
        {:else}
          <p class="next-review">Add some flashcards to get started!</p>
        {/if}
      </div>
    {/if}
  {/if}
</div>

<style>
  .spaced-repetition-container {
    max-width: 1400px;
    margin: 0 auto;
    padding: 2rem;
    width: 100%;
  }

  .stats-bar {
    display: flex;
    gap: 1.5rem;
    align-items: center;
    padding: 1rem;
    background: #ffffff;
    border: 1px solid #e9ecef;
    border-radius: 8px;
    margin-bottom: 2rem;
    flex-wrap: wrap;
    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  }

  .stat {
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
  }

  .stat-label {
    font-size: 0.75rem;
    color: #6c757d;
    text-transform: uppercase;
    letter-spacing: 0.05em;
  }

  .stat-value {
    font-size: 1.5rem;
    font-weight: bold;
    color: #212529;
  }

  .stat.due .stat-value {
    color: #dc3545;
  }

  .stat.new .stat-value {
    color: #0d6efd;
  }

  .stat.learning .stat-value {
    color: #ffc107;
  }

  .stat.review .stat-value {
    color: #198754;
  }

  .stat.cram .stat-value {
    color: #6f42c1;
  }

  .stat.again .stat-value {
    color: #fd7e14;
    animation: pulse 2s ease-in-out infinite;
  }

  @keyframes pulse {
    0%, 100% {
      opacity: 1;
    }
    50% {
      opacity: 0.6;
    }
  }

  .mode-btn {
    padding: 0.5rem 1rem;
    background: #f8f9fa;
    color: #212529;
    border: 1px solid #dee2e6;
    border-radius: 4px;
    cursor: pointer;
    font-size: 0.875rem;
    transition: all 0.2s;
  }

  .mode-btn:hover {
    background: #e9ecef;
    border-color: #adb5bd;
  }

  .mode-btn.active {
    background: #6f42c1;
    color: #fff;
    border-color: #6f42c1;
    box-shadow: 0 0 10px rgba(111, 66, 193, 0.2);
  }

  .mode-btn.active:hover {
    background: #5a32a3;
  }

  .view-toggle-btn {
    padding: 0.5rem 1rem;
    background: #f8f9fa;
    color: #212529;
    border: 1px solid #dee2e6;
    border-radius: 4px;
    cursor: pointer;
    font-size: 0.875rem;
    transition: all 0.2s;
  }

  .view-toggle-btn:hover {
    background: #e9ecef;
    border-color: #adb5bd;
  }

  .view-toggle-btn.active {
    background: #0d6efd;
    color: #fff;
    border-color: #0d6efd;
  }

  .reset-btn {
    margin-left: auto;
    padding: 0.5rem 1rem;
    background: #f8f9fa;
    color: #dc3545;
    border: 1px solid #dee2e6;
    border-radius: 4px;
    cursor: pointer;
    font-size: 0.875rem;
    transition: background 0.2s;
  }

  .reset-btn:hover {
    background: #dc3545;
    color: #fff;
    border-color: #dc3545;
  }

  .cram-notice {
    text-align: center;
    padding: 0.75rem;
    background: #f8f4ff;
    border: 1px solid #e0cffc;
    border-radius: 6px;
    color: #6f42c1;
    font-size: 0.875rem;
    margin-bottom: 1rem;
  }

  .card-container {
    display: flex;
    justify-content: center;
    align-items: center;
    min-height: 500px;
  }

  .card {
    width: 100%;
    background: #ffffff;
    border: 1px solid #e9ecef;
    border-radius: 12px;
    padding: 2rem;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
  }

  .card-front, .card-back {
    margin-bottom: 2rem;
  }

  .card-label {
    font-size: 0.875rem;
    color: #6c757d;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    margin-bottom: 0.5rem;
  }

  .card-content {
    font-size: 1.25rem;
    line-height: 1.6;
    color: #212529;
    min-height: 80px;
  }

  .show-answer-btn {
    width: 100%;
    padding: 1rem;
    background: #0d6efd;
    color: white;
    border: none;
    border-radius: 8px;
    font-size: 1rem;
    font-weight: 600;
    cursor: pointer;
    transition: background 0.2s;
    margin-top: 1rem;
  }

  .show-answer-btn:hover {
    background: #0b5ed7;
  }

  .rating-buttons {
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    gap: 0.75rem;
    margin-top: 1.5rem;
  }

  .rating-btn {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.5rem;
    padding: 1rem 0.5rem;
    border: 2px solid;
    border-radius: 8px;
    cursor: pointer;
    font-weight: 600;
    transition: all 0.2s;
  }

  .rating-btn.again {
    background: #fff;
    border-color: #dc3545;
    color: #dc3545;
  }

  .rating-btn.again:hover {
    background: #dc3545;
    color: white;
  }

  .rating-btn.hard {
    background: #fff;
    border-color: #0dcaf0;
    color: #0dcaf0;
  }

  .rating-btn.hard:hover {
    background: #0dcaf0;
    color: white;
  }

  .rating-btn.good {
    background: #fff;
    border-color: #198754;
    color: #198754;
  }

  .rating-btn.good:hover {
    background: #198754;
    color: white;
  }

  .rating-btn.easy {
    background: #fff;
    border-color: #ffc107;
    color: #ffc107;
  }

  .rating-btn.easy:hover {
    background: #ffc107;
    color: #212529;
  }

  .rating-label {
    font-size: 1rem;
  }

  .rating-time {
    font-size: 0.75rem;
    opacity: 0.8;
  }

  .card-metadata {
    margin-top: 2rem;
    padding-top: 1rem;
    border-top: 1px solid #dee2e6;
  }

  .tags {
    display: flex;
    gap: 0.5rem;
    margin-bottom: 0.75rem;
    flex-wrap: wrap;
  }

  .tag {
    padding: 0.25rem 0.75rem;
    background: #e9ecef;
    color: #495057;
    border-radius: 12px;
    font-size: 0.75rem;
  }

  .card-info {
    display: flex;
    gap: 1.5rem;
    font-size: 0.875rem;
    color: #6c757d;
    align-items: center;
    flex-wrap: wrap;
  }

  .edit-btn {
    display: inline-flex;
    align-items: center;
    gap: 0.25rem;
    padding: 0.25rem 0.75rem;
    background: #f8f9fa;
    color: #0d6efd;
    border: 1px solid #dee2e6;
    border-radius: 4px;
    text-decoration: none;
    font-size: 0.75rem;
    transition: all 0.2s;
    margin-left: auto;
  }

  .edit-btn:hover {
    background: #0d6efd;
    color: #fff;
    border-color: #0d6efd;
  }

  .references-section {
    margin-top: 1rem;
  }

  .references-toggle {
    display: flex;
    align-items: center;
    justify-content: space-between;
    width: 100%;
    padding: 0.75rem 1rem;
    background: #f8f9fa;
    border: 1px solid #dee2e6;
    border-radius: 6px;
    color: #495057;
    font-size: 0.875rem;
    font-weight: 600;
    cursor: pointer;
    transition: all 0.2s;
  }

  .references-toggle:hover {
    background: #e9ecef;
    border-color: #adb5bd;
  }

  .toggle-icon {
    font-size: 0.75rem;
    margin-left: 0.5rem;
  }

  .references-content {
    margin-top: 0.5rem;
    padding: 1rem;
    background: #ffffff;
    border: 1px solid #dee2e6;
    border-radius: 6px;
  }

  .references-list {
    list-style: none;
    padding: 0;
    margin: 0;
  }

  .reference-item {
    padding: 0.5rem 0;
    border-bottom: 1px solid #f8f9fa;
  }

  .reference-item:last-child {
    border-bottom: none;
  }

  .reference-link {
    color: #0d6efd;
    text-decoration: none;
    font-size: 0.875rem;
    transition: color 0.2s;
    word-break: break-all;
  }

  .reference-link:hover {
    color: #0a58ca;
    text-decoration: underline;
  }

  .reference-text {
    color: #495057;
    font-size: 0.875rem;
  }

  .no-cards {
    text-align: center;
    padding: 4rem 2rem;
  }

  .no-cards h2 {
    font-size: 2rem;
    margin-bottom: 1rem;
    color: #212529;
  }

  .no-cards p {
    font-size: 1.125rem;
    color: #6c757d;
    margin-bottom: 0.5rem;
  }

  .next-review {
    margin-top: 1rem;
    font-style: italic;
  }

  /* Manage View Styles */
  .manage-view {
    width: 100%;
  }

  .table-controls {
    display: flex;
    gap: 1rem;
    align-items: center;
    margin-bottom: 1.5rem;
    flex-wrap: wrap;
  }

  .search-input {
    flex: 1;
    min-width: 250px;
    padding: 0.75rem 1rem;
    background: #ffffff;
    border: 1px solid #ced4da;
    border-radius: 6px;
    color: #212529;
    font-size: 0.875rem;
    transition: border-color 0.2s;
  }

  .search-input:focus {
    outline: none;
    border-color: #0d6efd;
    box-shadow: 0 0 0 0.2rem rgba(13, 110, 253, 0.25);
  }

  .search-input::placeholder {
    color: #adb5bd;
  }

  .table-info {
    color: #6c757d;
    font-size: 0.875rem;
  }

  .table-container {
    overflow-x: auto;
    background: #ffffff;
    border-radius: 8px;
    border: 1px solid #dee2e6;
  }

  .flashcard-table {
    width: 100%;
    border-collapse: collapse;
    font-size: 0.875rem;
  }

  .flashcard-table thead {
    background: #f8f9fa;
    position: sticky;
    top: 0;
    z-index: 1;
  }

  .flashcard-table th {
    padding: 1rem;
    text-align: left;
    font-weight: 600;
    color: #495057;
    border-bottom: 2px solid #dee2e6;
    white-space: nowrap;
  }

  .flashcard-table th.sortable {
    cursor: pointer;
    user-select: none;
    transition: color 0.2s;
  }

  .flashcard-table th.sortable:hover {
    color: #212529;
  }

  .flashcard-table tbody tr {
    border-bottom: 1px solid #e9ecef;
    transition: background 0.2s;
  }

  .flashcard-table tbody tr:hover {
    background: #f8f9fa;
  }

  .flashcard-table tbody tr.due-now {
    background: rgba(220, 53, 69, 0.05);
  }

  .flashcard-table tbody tr.due-now:hover {
    background: rgba(220, 53, 69, 0.1);
  }

  .flashcard-table td {
    padding: 0.75rem 1rem;
    color: #212529;
  }

  .cell-id {
    font-family: monospace;
    color: #6c757d;
    font-size: 0.8rem;
  }

  .cell-front {
    max-width: 300px;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .cell-tags {
    display: flex;
    gap: 0.25rem;
    flex-wrap: wrap;
  }

  .tag-badge {
    padding: 0.2rem 0.5rem;
    background: #e9ecef;
    color: #495057;
    border-radius: 4px;
    font-size: 0.7rem;
  }

  .due-badge {
    padding: 0.25rem 0.5rem;
    border-radius: 4px;
    font-size: 0.75rem;
    white-space: nowrap;
  }

  .due-badge.overdue {
    background: rgba(220, 53, 69, 0.15);
    color: #dc3545;
    font-weight: 600;
  }

  .state-badge {
    padding: 0.25rem 0.5rem;
    border-radius: 4px;
    font-size: 0.75rem;
    font-weight: 600;
    text-align: center;
    display: inline-block;
    min-width: 70px;
  }

  .state-badge.state-0 {
    background: rgba(13, 202, 240, 0.15);
    color: #0dcaf0;
  }

  .state-badge.state-1 {
    background: rgba(255, 193, 7, 0.15);
    color: #ffc107;
  }

  .state-badge.state-2 {
    background: rgba(25, 135, 84, 0.15);
    color: #198754;
  }

  .state-badge.state-3 {
    background: rgba(220, 53, 69, 0.15);
    color: #dc3545;
  }

  .cell-difficulty,
  .cell-stability,
  .cell-reps {
    font-variant-numeric: tabular-nums;
    text-align: right;
  }

  @media (max-width: 640px) {
    .stats-bar {
      gap: 1rem;
    }

    .rating-buttons {
      grid-template-columns: repeat(2, 1fr);
    }

    .card-info {
      flex-direction: column;
      gap: 0.5rem;
    }

    .spaced-repetition-container {
      max-width: 100%;
      padding: 1rem;
    }

    .table-container {
      font-size: 0.75rem;
    }

    .flashcard-table th,
    .flashcard-table td {
      padding: 0.5rem;
    }
  }
</style>
