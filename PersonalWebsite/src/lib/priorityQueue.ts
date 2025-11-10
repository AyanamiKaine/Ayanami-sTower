/**
 * Priority Queue for Spaced Repetition Cards
 * 
 * Manages flashcards based on their due dates, ensuring cards
 * that are due sooner are reviewed first.
 */

export interface FlashcardData {
  id: string;
  front: string;
  back: string;
  tags: string[];
  card: any; // FSRS Card object
  due: Date;
  source?: string; // Optional source filename for GitHub edit link
  references?: string[]; // Optional references to support the answer
  priority?: number; // Custom priority (higher = higher priority, default = 0)
  type?: 'flashcard' | 'quiz'; // Item type (default: flashcard)
  question?: string; // Quiz question
  options?: string[]; // Quiz answer options
  correctIndex?: number; // Quiz correct answer index
  explanation?: string; // Quiz explanation for the correct answer
}

export class PriorityQueue {
  private heap: FlashcardData[] = [];

  /**
   * Add a flashcard to the queue
   */
  enqueue(item: FlashcardData): void {
    this.heap.push(item);
    this.bubbleUp(this.heap.length - 1);
  }

  /**
   * Remove and return the flashcard with the earliest due date
   */
  dequeue(): FlashcardData | null {
    if (this.heap.length === 0) return null;
    if (this.heap.length === 1) return this.heap.pop()!;

    const root = this.heap[0];
    this.heap[0] = this.heap.pop()!;
    this.bubbleDown(0);
    return root;
  }

  /**
   * View the next flashcard without removing it
   */
  peek(): FlashcardData | null {
    return this.heap.length > 0 ? this.heap[0] : null;
  }

  /**
   * Get all cards due for review (due date <= now)
   */
  getDueCards(now: Date = new Date()): FlashcardData[] {
    return this.heap.filter(card => card.due <= now);
  }

  /**
   * Get the total number of cards in the queue
   */
  size(): number {
    return this.heap.length;
  }

  /**
   * Check if the queue is empty
   */
  isEmpty(): boolean {
    return this.heap.length === 0;
  }

  /**
   * Update a card in the queue (remove old, add new)
   */
  updateCard(id: string, newCard: FlashcardData): void {
    const index = this.heap.findIndex(card => card.id === id);
    if (index !== -1) {
      this.heap.splice(index, 1);
      this.enqueue(newCard);
    }
  }

  /**
   * Get all cards (for persistence)
   */
  getAllCards(): FlashcardData[] {
    return [...this.heap];
  }

  /**
   * Load cards from array
   */
  loadCards(cards: FlashcardData[]): void {
    this.heap = [];
    cards.forEach(card => {
      // Ensure due is a Date object
      card.due = new Date(card.due);
      this.enqueue(card);
    });
  }

  /**
   * Get the priority (position) of a card by its ID
   * Returns 1-based position (1 = highest priority)
   */
  getCardPriority(id: string): number | null {
    // Sort all cards using the same comparison logic as the heap
    const sorted = [...this.heap].sort((a, b) => this.compare(a, b));
    const index = sorted.findIndex(card => card.id === id);
    return index !== -1 ? index + 1 : null;
  }

  /**
   * Compare two cards for priority ordering
   * First compare by due date (earlier = higher priority)
   * For cards with the same due status, higher priority number comes first
   */
  private compare(a: FlashcardData, b: FlashcardData): number {
    const now = new Date();
    const aDue = a.due.getTime();
    const bDue = b.due.getTime();
    const nowTime = now.getTime();
    
    // Check if cards are due or not
    const aIsDue = aDue <= nowTime;
    const bIsDue = bDue <= nowTime;
    
    // If one is due and the other isn't, the due one comes first
    if (aIsDue && !bIsDue) return -1;
    if (!aIsDue && bIsDue) return 1;
    
    // Both are due or both are not due
    if (aIsDue && bIsDue) {
      // Both are due: use priority first, then due date
      const priorityA = a.priority ?? 0;
      const priorityB = b.priority ?? 0;
      
      if (priorityA !== priorityB) {
        return priorityB - priorityA; // Higher priority number comes first
      }
      
      return aDue - bDue; // Earlier due date comes first
    } else {
      // Neither is due: sort by due date only
      return aDue - bDue; // Earlier due date comes first (for future reviews)
    }
  }

  private bubbleUp(index: number): void {
    while (index > 0) {
      const parentIndex = Math.floor((index - 1) / 2);
      if (this.compare(this.heap[index], this.heap[parentIndex]) >= 0) break;
      
      [this.heap[index], this.heap[parentIndex]] = [this.heap[parentIndex], this.heap[index]];
      index = parentIndex;
    }
  }

  private bubbleDown(index: number): void {
    while (true) {
      let smallest = index;
      const leftChild = 2 * index + 1;
      const rightChild = 2 * index + 2;

      if (leftChild < this.heap.length && this.compare(this.heap[leftChild], this.heap[smallest]) < 0) {
        smallest = leftChild;
      }
      if (rightChild < this.heap.length && this.compare(this.heap[rightChild], this.heap[smallest]) < 0) {
        smallest = rightChild;
      }
      if (smallest === index) break;

      [this.heap[index], this.heap[smallest]] = [this.heap[smallest], this.heap[index]];
      index = smallest;
    }
  }
}
