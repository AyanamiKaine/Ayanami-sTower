local StellaSTL = require "StellaSTL"
local StellaSockets = StellaSTL.StellaSockets

local url = "ipc:///test"

local Server = StellaSockets.Pull.new(url, 100)
local Client = StellaSockets.Push.new(url, 100)

local function serverLoop()
    while true do
        local rv = nil
        while not rv do
            rv = Client:send("DATA FROM PUSHER") -- Non-blocking send
            coroutine.yield()
        end
        coroutine.yield()
    end
end

-- Client Coroutine
local function clientLoop(message_handler)
    while true do
        local string_message = nil
        while not string_message do
            string_message = Server:receive() -- Non-blocking receive
            coroutine.yield()
        end
        message_handler(string_message)
        coroutine.yield()
    end
end

-- Create and Start Coroutines
local serverCo = coroutine.create(serverLoop)
local clientCo = coroutine.create(function() clientLoop(print) end)


while true do
    coroutine.resume(serverCo)
    coroutine.resume(clientCo)
end