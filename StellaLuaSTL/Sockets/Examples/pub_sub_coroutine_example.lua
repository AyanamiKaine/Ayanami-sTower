local StellaSTL = require "StellaSTL"
local StellaSockets = StellaSTL.StellaSockets

local url = "ipc:///test"


local Publisher = StellaSockets.Publisher.new(url, 100)

local SubscriberA = StellaSockets.Subscriber.new(url, 100)
local SubscriberB = StellaSockets.Subscriber.new(url, 100)

SubscriberA:subscribe("TEST")
SubscriberB:subscribe("TEST")

-- Server Coroutine
local function serverLoop()
    while true do
        local rv = nil
        while not rv do
            rv = Publisher:send("DATA FROM PUBLISHER", "TEST") -- Non-blocking send
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
            string_message = SubscriberA:receive() -- Non-blocking receive
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
    coroutine.resume(clientCo)
    coroutine.resume(clientCo)
    coroutine.resume(clientCo)
    coroutine.resume(serverCo)
    coroutine.resume(clientCo)
    coroutine.resume(clientCo)
end
