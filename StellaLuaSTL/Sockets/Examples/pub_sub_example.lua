local StellaSTL = require "StellaSTL"
local StellaSockets = StellaSTL.StellaSockets

local url = "ipc:///test"


local Publisher = StellaSockets.Publisher.new(url)

local SubscriberA = StellaSockets.Subscriber.new(url)
local SubscriberB = StellaSockets.Subscriber.new(url)



SubscriberA:subscribe("TEST")
SubscriberB:subscribe("TEST")
local startTime = os.clock()

Publisher:send_blocking("Hello From Publisher", "TEST")

print(SubscriberA:receive_blocking()) -- Hello From Publisher
local endTime = os.clock()
print(SubscriberB:receive_blocking()) -- Hello From Publisher
local elapsed_time_seconds = endTime - startTime
local elapsed_time_nanoseconds = elapsed_time_seconds * 1e9
print("Elapsed time in nanoseconds:", elapsed_time_nanoseconds)
