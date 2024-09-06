package.path = package.path .. "C:\\Users\\ayanami\\Ayanami-sTower\\StellaLuaSTL\\?.lua"
local STL = require "StellaSTL"
local StellaSockets = STL.StellaSockets

local url = "ipc:///test"

local server = StellaSockets.Response.new(url)

-- 1. Open the file in read mode
local file = io.open("C:/Users/ayanami/AppData/Roaming/Stella Knowledge Manager/main_save_TEST_data.json", "r")
local json_string = {}

if file then
    -- 2. Read the entire file contents
    json_string = file:read("*a")
    -- 3. Close the file
    file:close()
else
    print("Error: Could not open the file.")
end

local database = STL.json.decode(json_string)



local function findTableById(targetId)
    for _, table in ipairs(database) do
        if table.Id == targetId then
            return table
        end
    end
    return nil -- Return nil if no table is found
end

local function save()
    local serialized_database = STL.json.encode(database)
    local file = io.open("C:/Users/ayanami/AppData/Roaming/Stella Knowledge Manager/main_save_TEST_data.json", "w")
    local json_string = {}

    if file then
        -- 2. Read the entire file contents
        file:write(serialized_database)
        -- 3. Close the file
        file:close()
    else
        print("Error: Could not open the file.")
    end
end

 
local function handleRequest(request)
    local response = ""

    if request.Command == "CREATE" then
        table.insert(database, request.FileToLearn)
        server:send("{'ok': 'file added'}")
    end
    if request.Command == "UPDATE" then
        --server:send("{'ok': 'file added'}")
    end
    if request.Command == "DELETE" then
        --server:send("{'ok': 'file added'}")
    end
    if request.Command == "RETRIVE_ALL_ITEMS" then
        local serialized_database = STL.json.encode(database)
        response = string.format('{"ok": "%s"}', serialized_database)
        server:send(response)
    end
end
local function countFields(table)
    local count = 0
    for _ in pairs(table) do
        count = count + 1
    end
    return count
end
save()
while true do
    local json_request = server:receive_blocking()
    local request = STL.json.decode(json_request)
    handleRequest(request)
    --print(database[#database].Id)
end