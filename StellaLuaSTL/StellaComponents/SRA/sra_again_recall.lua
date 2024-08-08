local STL = require "StellaSTL"
local StellaSockets = STL.StellaSockets
local json = STL.json
local DateTime = STL.StellaDataTime
local url = "ipc:///test"


local server = StellaSockets.Response.new(url)

--local json_string = server:receive_blocking()


local function handle_json_message(json_string)
    local data = json.decode(json_string)
    if data.EaseFactor and data.NumberOfTimeSeen then
        return data
    else 
        return {error = "Json String is invalid: " .. json_string}
    end
end



local function message_handler(message)
    local data = handle_json_message(message)

    if data.error then
        print(json.encode(data))
        --server:send(json.encode(data))
        return
    end

    local str_futureTime = DateTime.nowToIso8601(DateTime.AddMinutes(DateTime.now(), 1) )
    print(json.encode({ok = str_futureTime }))
    --server:send({ok = str_futureTime })
end

local dummy_data = [[

{
    "EaseFactor" : 2,
    "NumberOfTimeSeen" : 2
}
]]


message_handler(dummy_data)

--server:send("HELLO FROM SERVER!")
