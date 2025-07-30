export class SparsedSet {
    constructor(capacity, maxValue) {
        // Stores the actual elements
        this.dense = [];
        /* 
        This is like bit-vector where 
        we use elements as index. Here 
        values are not binary, but
        indexes of dense array.
        */
        this.sparse = [];
        // Maximum vlues this set can store. Size of sparse[] is equal to maxVal + 1
        this.maxValue = maxValue;
        // Capacity of the Set. Size of sparse is equal to capacity
        this.capacity = capacity;
        // Current number of elements in the set
        this.size = 0;
    }

    insert(value) {
        // Inserting into array-dense[] at index 'n'.
        this.dense[this.size] = value;

        // Mapping it to sparse[] array.
        this.sparse[value] = this.size;

        // Increment count of elements in set
        this.size += 1;
    }

    getIndex(value) {
        if (
            this.sparse[value] < this.size &&
            this.dense[this.sparse[value]] === value
        )
            return this.sparse[value];

        // Not found
        return -1;
    }

    has(value) {
        return (
            this.sparse[value] < this.size &&
            this.dense[this.sparse[value]] === value
        );
    }

    remove(value) {
        const index = this.getIndex(value);
        if (index === -1) return;
        const temp = this.dense[this.size - 1]; // Take an element from end
        this.dense[index] = temp; // Overwrite.
        this.sparse[temp] = index; // Overwrite.
        this.size--;
    }
}
