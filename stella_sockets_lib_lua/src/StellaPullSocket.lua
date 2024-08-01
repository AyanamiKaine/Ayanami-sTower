local ffi = require("ffi")
local StellaMessagingFFI = require("StellaMessagingFFI")  -- Load the module

local StellaPullSocket = {}

function StellaPullSocket.new(address)
    local self = {} -- Local table to hold the object's state

    -- Private members (only accessible within the module)
    local _socketHandler = StellaMessagingFFI.create_pull_socket();
    
    local c_str_address = ffi.new("char[?]", #address + 1)
    ffi.copy(c_str_address, address)
    StellaMessagingFFI.bind(_socketHandler, c_str_address);

    return self -- Return the object (table)
end

return StellaPullSocket