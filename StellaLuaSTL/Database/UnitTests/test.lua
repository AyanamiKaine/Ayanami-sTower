local StellaDatabase = require "Database.StellaDatabase"
local StellaTesting = require "StellaTesting.StellaTesting"
local json = require "Json.json"




StellaTesting.runTest("DB Creation", function()
    local db = StellaDatabase:new()
    StellaTesting.assertTrue(db, "Database new should create a table")
end)

StellaTesting.runTest("DB item insertion", function()
    local db = StellaDatabase:new()
    local expected = { name = "Tim", age = 20 }

    db:create("1", { name = "Tim", age = 20 })
    db:read("1")
    StellaTesting.assertEqual(expected.name, db:read("1").name, "A new entry in the database should exist")
end)

-- Add more tests...

StellaTesting.report() -- Show the results

--print(json.encode(db))
