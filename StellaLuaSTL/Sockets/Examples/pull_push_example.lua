local StellaSTL = require "StellaSTL"
local StellaSockets = StellaSTL.StellaSockets

local url = "ipc:///test"

local Server = StellaSockets.Pull.new(url)
local Client = StellaSockets.Push.new(url)

Client:send_blocking("Hello From Client!")
print(Server:receive_blocking())

