package;

class ComponentStorage<T> {
    public var dense:Array<T>;
    public var entityIdMap:Array<Int>;
    public var sparse:Array<Null<Int>>;
    public var size:Int = 0;
    public var capacity:Int;
    
    public function new(capacity:Int = 100000) {
        this.capacity = capacity;
        this.dense = [];
        this.dense.resize(capacity);
        this.entityIdMap = [];
        this.entityIdMap.resize(capacity);
        this.sparse = [];
    }
    
    public function has(entity:Entity):Bool {
        if (entity.id >= sparse.length) return false;
        var sparseIdx = sparse[entity.id];
        return sparseIdx != null && entityIdMap[sparseIdx] == entity.id;
    }
    
    public function get(entity:Entity):Null<T> {
        if (!has(entity)) {
            return null;
        }
        var denseIdx = sparse[entity.id];
        return dense[denseIdx];
    }
    
    public function set(entity:Entity, component:T):Void {
        if (has(entity)) {
            var denseIdx = sparse[entity.id];
            dense[denseIdx] = component;
            return;
        }
        
        if (size >= capacity) {
            trace("Component storage is full.");
            return;
        }
        
        // Ensure sparse array is large enough
        while (sparse.length <= entity.id) {
            sparse.push(null);
        }
        
        var denseIdx = size;
        dense[denseIdx] = component;
        entityIdMap[denseIdx] = entity.id;
        sparse[entity.id] = denseIdx;
        size++;
    }
    
    public function remove(entity:Entity):Void {
        if (!has(entity)) {
            return;
        }
        
        var denseIdxToRemove = sparse[entity.id];
        var lastDenseIdx = size - 1;
        var lastEntityId = entityIdMap[lastDenseIdx];
        
        dense[denseIdxToRemove] = dense[lastDenseIdx];
        entityIdMap[denseIdxToRemove] = entityIdMap[lastDenseIdx];
        sparse[lastEntityId] = denseIdxToRemove;
        sparse[entity.id] = null;
        size--;
    }
    
    public function entityIds():Iterator<Int> {
        return new ComponentStorageEntityIdIterator(this);
    }
    
    public function components():Iterator<T> {
        return new ComponentStorageComponentIterator(this);
    }
    
    public function entries():Iterator<{entityId:Int, component:T}> {
        return new ComponentStorageEntryIterator(this);
    }
}

class ComponentStorageEntityIdIterator<T> {
    public var storage:ComponentStorage<T>;
    public var index:Int = 0;
    
    public function new(storage:ComponentStorage<T>) {
        this.storage = storage;
    }
    
    public function hasNext():Bool {
        return index < storage.size;
    }
    
    public function next():Int {
        return storage.entityIdMap[index++];
    }
}

class ComponentStorageComponentIterator<T> {
    public var storage:ComponentStorage<T>;
    public var index:Int = 0;
    
    public function new(storage:ComponentStorage<T>) {
        this.storage = storage;
    }
    
    public function hasNext():Bool {
        return index < storage.size;
    }
    
    public function next():T {
        return storage.dense[index++];
    }
}

class ComponentStorageEntryIterator<T> {
    public var storage:ComponentStorage<T>;
    public var index:Int = 0;
    
    public function new(storage:ComponentStorage<T>) {
        this.storage = storage;
    }
    
    public function hasNext():Bool {
        return index < storage.size;
    }
    
    public function next():{entityId:Int, component:T} {
        var i = index++;
        return {
            entityId: storage.entityIdMap[i],
            component: storage.dense[i]
        };
    }
}