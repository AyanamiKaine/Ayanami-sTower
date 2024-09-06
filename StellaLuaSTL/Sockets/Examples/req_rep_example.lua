local StellaSTL = require "StellaSTL"
local StellaSockets = StellaSTL.StellaSockets

local url = "ipc:///test"

local server = StellaSockets.Response.new(url)
local client = StellaSockets.Request.new(url)


--client:send_blocking("HELLO FROM CLIENT!")
while true do
    print(server:receive_blocking())
    server:send("HELLO FROM SERVER!") 
end
--print(client:receive_blocking())