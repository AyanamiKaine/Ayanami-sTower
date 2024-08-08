local StellaSTL = require "StellaSTL"
local StellaSockets = StellaSTL.StellaSockets

local jit = require "jit"

local url = "ipc:///test"

local server = StellaSockets.Response.new(url, 100)
local client = StellaSockets.Request.new(url, 100)

-- Server Coroutine
local function serverLoop(message_handler)
    while true do
        local data = nil
        while not data do
            data = server:receive() -- Non-blocking receive
            coroutine.yield() 
        end
        
        message_handler(data)
        server:send(data) -- Non-blocking send
        coroutine.yield() 
    end
end

-- Client Coroutine
local function clientLoop(message_handler)
    while true do
        local data = nil
        client:send("data") -- Non-blocking send
        while not data do
            data = client:receive() -- Non-blocking receive
            coroutine.yield()
        end
        message_handler(data)
        coroutine.yield()
    end
end

local function test(message)
    local m = message .. "s"
end

-- Create and Start Coroutines
local serverCo = coroutine.create(function() serverLoop(print) end)
local clientCo = coroutine.create(function() clientLoop(print) end)


--coroutine.resume(clientCo)
while true do
    coroutine.resume(clientCo)
    coroutine.resume(serverCo)
    --print("sss")
end

-- Main Loop (to keep the program running)

-- This loop keeps the script alive and allows coroutines to run
--coroutine.resume(clientCo)


