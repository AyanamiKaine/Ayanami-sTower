local ffi = require("ffi")
local StellaMessagingFFI = require("StellaMessagingFFI")  -- Load the module

local StellaResponseSocket = {}

function StellaResponseSocket.new(address)
    local self = {} -- Local table to hold the object's state

    -- Private members (only accessible within the module)
    local _socketHandler = StellaMessagingFFI.create_reponse_socket();
    
    local c_str_address = ffi.new("char[?]", #address + 1)
    ffi.copy(c_str_address, address)
    StellaMessagingFFI.socket_bind(_socketHandler, c_str_address);

    function self:send(message)
        local c_str = ffi.new("char[?]", #message + 1)
        ffi.copy(c_str, message)
        StellaMessagingFFI.socket_send_string_message(_socketHandler, c_str)
    end

    function self:send_no_block(message)
        local c_str = ffi.new("char[?]", #message + 1)
        ffi.copy(c_str, message)
        StellaMessagingFFI.socket_send_string_message_no_block(_socketHandler, c_str)
    end

    function self:Receive()
        local message = ffi.string(StellaMessagingFFI.socket_receive_string_message(_socketHandler));
        return message
    end

    return self -- Return the object (table)
end

return StellaResponseSocket