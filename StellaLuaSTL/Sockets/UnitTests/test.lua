local STL = require "StellaSTL"
local StellaSockets = STL.StellaSockets
local StellaTesting = STL.StellaTesting
local url = "ipc:///req_rep_unit_test"

local server = StellaSockets.Response.new(url, 10)
local client = StellaSockets.Request.new(url, 10)


StellaTesting.runTest("Basic Addition", function()
    client:send_blocking("Hello World")
    StellaTesting.assertEqual("Hello World", server:receive_blocking(),
        "Response Server Should Receive the message 'Hello World'")
    
    server:send_blocking("Hello World")
    StellaTesting.assertEqual("Hello World", client:receive_blocking(), 
        "Response Client Should Receive the message 'Hello World'")

end)