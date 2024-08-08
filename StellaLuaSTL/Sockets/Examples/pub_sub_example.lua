local StellaSTL = require "StellaSTL"
local StellaSockets = StellaSTL.StellaSockets

local url = "ipc:///test"


local Publisher = StellaSockets.Publisher.new(url)

local SubscriberA = StellaSockets.Subscriber.new(url)
local SubscriberB = StellaSockets.Subscriber.new(url)

SubscriberA:subscribe("TEST")
SubscriberB:subscribe("TEST")

Publisher:send_blocking("Hello From Publisher", "TEST")

print(SubscriberA:receive_blocking()) -- Hello From Publisher
print(SubscriberB:receive_blocking()) -- Hello From Publisher