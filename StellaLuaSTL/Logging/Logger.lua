local STL = require "StellaSTL"
local json = STL.json
local Logger = {}


local function nowToIso8601()
    local timestamp = os.time()
    local isoString = os.date("!%Y-%m-%dT%H:%M:%SZ", timestamp)
    return isoString
end

function Logger.Info(message, Sender)
    local date_time = nowToIso8601()

    local json_struct = {
        LogType = "Info",
        LogTime = date_time,
        LogMessage = message, -- Now using the 'message' argument
        Sender = Sender
    }

    local json_string = json.encode(json_struct);
    return json_string
end

function Logger.Error(message, Sender)
    local date_time = nowToIso8601()

    local json_struct = {
        LogType = "Error",
        LogTime = date_time,
        LogMessage = message, -- Now using the 'message' argument
        Sender = Sender
    }

    local json_string = json.encode(json_struct);
    return json_string
end

function Logger.Warning(message, Sender)
    local date_time = nowToIso8601()

    local json_struct = {
        LogType = "Warning",
        LogTime = date_time,
        LogMessage = message, -- Now using the 'message' argument
        Sender = Sender
    }

    local json_string = json.encode(json_struct);
    return json_string
end

function Logger.Critical(message, Sender)
    local date_time = nowToIso8601()

    local json_struct = {
        LogType = "Critical",
        LogTime = date_time,
        LogMessage = message, -- Now using the 'message' argument
        Sender = Sender
    }

    local json_string = json.encode(json_struct);
    return json_string
end

function Logger.Debug(message, Sender)
    local date_time = nowToIso8601()

    local json_struct = {
        LogType = "Debug",
        LogTime = date_time,
        LogMessage = message, -- Now using the 'message' argument
        Sender = Sender
    }

    local json_string = json.encode(json_struct);
    return json_string
end

return Logger