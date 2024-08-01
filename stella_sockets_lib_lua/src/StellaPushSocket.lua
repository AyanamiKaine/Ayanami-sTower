local ffi = require("ffi")
local StellaMessagingFFI = require("StellaMessagingFFI")  -- Load the module

local StellaPushSocket = {}

function StellaPushSocket.new(address)
    local self = {} -- Local table to hold the object's state

    -- Private members (only accessible within the module)
    local _socketHandler = StellaMessagingFFI.create_push_socket();
    
    local c_str_address = ffi.new("char[?]", #address + 1)
    ffi.copy(c_str_address, address)
    StellaMessagingFFI.socket_connect(_socketHandler, c_str_address);

    function self:send(message)
        local c_str = ffi.new("char[?]", #message + 1)
        ffi.copy(c_str, message)
        StellaMessagingFFI.socket_send_string_message(_socketHandler, c_str)
    end
    
    -- This immededially returns and if the message couldnt be send it gets discarded
    function self:send_no_block(message)
        local c_str = ffi.new("char[?]", #message + 1)
        ffi.copy(c_str, message)
        StellaMessagingFFI.socket_send_string_message_no_block(_socketHandler, c_str)
    end

    return self -- Return the object (table)
end

return StellaPushSocket