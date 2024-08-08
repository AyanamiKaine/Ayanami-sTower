--[[
  Module: StellaDatabase
  Description: A simple in-memory key-value database with JSON serialization.

  Methods:
    * new() -> StellaDatabase: Creates a new instance of the database.
    * create(id, data) -> (boolean, string|nil): Creates a new record with the given ID and data. Returns true on success, false and an error message on failure.
    * read(id) -> (any, string|nil): Reads the record with the given ID. Returns the data on success, false and an error message on failure.
    * update(id, new_data) -> (boolean, string|nil): Updates the record with the given ID and new data. Returns true on success, false and an error message on failure.
    * delete(id) -> (boolean, string|nil): Deletes the record with the given ID. Returns true on success, false and an error message on failure.
    * to_json() -> string: Serializes the entire database to JSON.
    * from_json(json_string): Deserializes the database from JSON.
    * reset() -> (boolean): Deletes all records, returning true on success.
]]

--local StellaSTL = require "StellaSTL" -- Placeholder for potential future use
local StellaDatabase = {}
local json = require "./json/json"
StellaDatabase.__index = StellaDatabase

--[[
  Constructor: new
  Returns: A new instance of StellaDatabase
]]
function StellaDatabase:new()
    local self = setmetatable({}, StellaDatabase)
    self.data = {} -- Initialize the data store within each instance
    return self
end

--[[
  Create a new record
  id: Unique identifier for the record
  data: The data to store (can be any Lua data structure)
]]
function StellaDatabase:create(id, data)
    if self.data[id] then
        return false, "Record with ID already exists"
    end

    self.data[id] = data
    return true
end

--[[
  Read a record by ID
  id: Unique identifier for the record to read
]]
function StellaDatabase:read(id)
    if not self.data[id] then
        return false, "Record not found"
    end
    return self.data[id]
end

-- db:update("user123", {name = "Alice", age = 31})
function StellaDatabase:update(id, new_data)
    if not self.data[id] then
        return false, "Record not found"
    end

    self.data[id] = new_data
    return true
end

-- db:delete("user123")
function StellaDatabase:delete(id)
    if not self.data[id] then
        return false, "Record not found"
    end

    self.data[id] = nil
    return true
end

function StellaDatabase:to_json()
    return json.encode(self.data)
end

function StellaDatabase:from_json(json_string)
    self.data = json.decode(json_string)
end

function StellaDatabase:reset()
    self.data = {} -- Clear the data store, effectively resetting the database
    return true
end

return StellaDatabase

