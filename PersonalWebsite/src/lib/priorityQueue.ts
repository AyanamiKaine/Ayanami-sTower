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

  private bubbleUp(index: number): void {
    while (index > 0) {
      const parentIndex = Math.floor((index - 1) / 2);
      if (this.heap[index].due >= this.heap[parentIndex].due) break;
      
      [this.heap[index], this.heap[parentIndex]] = [this.heap[parentIndex], this.heap[index]];
      index = parentIndex;
    }
  }

  private bubbleDown(index: number): void {
    while (true) {
      let smallest = index;
      const leftChild = 2 * index + 1;
      const rightChild = 2 * index + 2;

      if (leftChild < this.heap.length && this.heap[leftChild].due < this.heap[smallest].due) {
        smallest = leftChild;
      }
      if (rightChild < this.heap.length && this.heap[rightChild].due < this.heap[smallest].due) {
        smallest = rightChild;
      }
      if (smallest === index) break;

      [this.heap[index], this.heap[smallest]] = [this.heap[smallest], this.heap[index]];
      index = smallest;
    }
  }
}
