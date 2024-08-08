# StellaDatabase: A Simple Lua In-Memory Database

StellaDatabase is a lightweight, easy-to-use in-memory key-value database for Lua applications. It provides basic CRUD (Create, Read, Update, Delete) operations and supports JSON serialization for data persistence.

## Features

- **In-Memory Storage:** Data is stored directly in memory for fast access.
- **Key-Value Model:** Records are accessed by unique IDs.
- **JSON Serialization:** Easily save and load your database to/from JSON format.
- **Simple API:** Straightforward methods for common database operations.

## Usage

1. **Require the Module:**

   ```lua
   local StellaDatabase = require("StellaDatabase")  -- Adjust path as needed
   ```

2. **Create a Database Instance:**

   ```lua
   local db = StellaDatabase:new()
   ```

3. **Interact with the Database:**

   ```lua
   -- Create a Record
   local success, err = db:create("user123", {name = "Alice", age = 30})

   -- Read a Record
   local data, err = db:read("user123")

   -- Update a Record
   db:update("user123", {name = "Alice", age = 31})

   -- Delete a Record
   db:delete("user123")

   -- Serialize to JSON
   local json_data = db:to_json()

   -- Deserialize from JSON
   db:from_json(json_data)

   --Reset the database
   db:reset()
   ```

## API Reference

### `StellaDatabase:new()`

Creates and returns a new instance of the `StellaDatabase` class.

### `StellaDatabase:create(id, data)`

Creates a new record with the specified `id` and `data`. Returns `true` on success, or `false` and an error message if a record with the same ID already exists.

### `StellaDatabase:read(id)`

Reads the record associated with the given `id`. Returns the data if found, or `false` and an error message if not found.

### `StellaDatabase:update(id, new_data)`

Updates the data of the record with the specified `id`. Returns `true` on success, or `false` and an error message if the record does not exist.

### `StellaDatabase:delete(id)`

Deletes the record with the specified `id`. Returns `true` on success, or `false` and an error message if the record does not exist.

### `StellaDatabase:to_json()`

Serializes the entire database contents to a JSON string.

### `StellaDatabase:from_json(json_string)`

Deserializes data from the given JSON string and populates the database.

### `StellaDatabase:reset()`

Deletes all records from the database, returning `true` on success.

## Limitations

- **In-Memory Only:** Data is not persistent across application restarts.
- **No Advanced Features:** Lacks complex querying, indexing, or transaction support.

## Example

```lua
local db = StellaDatabase:new()
db:create("product1", {name = "Widget", price = 19.99})
db:create("product2", {name = "Gadget", price = 49.95})

print(db:read("product1"))  -- Output: {name = "Widget", price = 19.99}
```
